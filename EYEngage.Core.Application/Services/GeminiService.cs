using System.Net.Http.Json;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EYEngage.Core.Application.Dto.EventDto;
using EYEngage.Core.Domain;
using Microsoft.Extensions.Configuration;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace EYEngage.Core.Application.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public GeminiService(IConfiguration config, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"];
        _baseUrl = config["Gemini:BaseUrl"];
    }

    public async Task<string> GenerateReport(EventAnalyticsDto analytics)
    {
        var prompt = BuildPrompt(analytics);
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var url = $"{_baseUrl}?key={_apiKey}";
        var response = await _httpClient.PostAsJsonAsync(url, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        return result?.candidates[0]?.content?.parts[0]?.text ?? "No response generated.";
    }

    private string BuildPrompt(EventAnalyticsDto analytics)
    {
        return $@"
Vous êtes un analyste senior chez EY. Génerez un rapport professionnel basé sur les données suivantes.
Utilisez le format Markdown avec mise en forme appropriée.

**Consignes:**
- Langue: Français
- Ton: Professionnel et concis
- Structure:
  1. **Résumé Exécutif** (3-4 lignes max)
  2. **Performances Clés** (statistiques importantes en gras)
  3. **Analyse par Département** (points forts/faibles)
  4. **Tendances** (évolution mensuelle)
  5. **Recommandations** (3-5 suggestions actionnables)

**Données:**
**Événements Totaux:** {analytics.TotalEvents}
**Participants Totaux:** {analytics.TotalParticipants}
**Taux de Participation Moyen:** {analytics.AvgParticipationPerEvent:F1}
**Taux de Conversion:** {(analytics.ParticipationRate * 100):F1}%

**Top 3 Événements:**
{string.Join("\n", analytics.PopularEvents.Take(3).Select(e => $"- **{e.Title}**: {e.Participants} participants, {e.Interests} intéressés"))}

**Répartition Départementale:**
{string.Join("\n", analytics.DepartmentStats.Select(d => $"- **{d.DepartmentName}**: {d.TotalEvents} événements ({d.TotalParticipants} participants)"))}

**Tendance Mensuelle (6 derniers mois):**
{string.Join("\n", analytics.MonthlyStats.TakeLast(6).Select(m => $"- **{m.Month}/{m.Year}**: {m.EventsCount} événements, {m.ParticipantsCount} participants"))}

Ajoutez des insights stratégiques basés sur les données. Mettez en valeur les chiffres importants en **gras**.
";
    }

    public string ExtractTextFromPdf(Stream pdfStream)
    {
        try
        {
            using (var reader = new PdfReader(pdfStream))
            using (var pdfDoc = new PdfDocument(reader))
            {
                var text = new StringBuilder();
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    var page = pdfDoc.GetPage(i);
                    text.Append(PdfTextExtractor.GetTextFromPage(page));
                }
                return text.ToString();
            }
        }
        catch (Exception ex)
        {
            // En cas d'erreur, retourner un texte vide plutôt que de faire planter l'application
            return string.Empty;
        }
    }

    public async Task<List<CandidateRecommendation>> GetTopCandidatesAsync(JobOffer jobOffer, List<ApplicationData> applications)
    {
        try
        {
            var prompt = BuildRecommendationPrompt(jobOffer, applications);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var url = $"{_baseUrl}?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var responseText = result?.candidates[0]?.content?.parts[0]?.text;

            return ParseRecommendations(responseText, applications);
        }
        catch (Exception ex)
        {
            // En cas d'erreur avec l'API, retourner une liste basique basée sur l'ordre d'application
            return CreateFallbackRecommendations(applications);
        }
    }

    private string BuildRecommendationPrompt(JobOffer jobOffer, List<ApplicationData> applications)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Vous êtes un expert en recrutement chez EY. Analysez les candidatures pour le poste suivant :");
        prompt.AppendLine($"**Titre du poste:** {jobOffer.Title}");
        prompt.AppendLine($"**Description:** {jobOffer.Description}");
        prompt.AppendLine($"**Compétences clés:** {jobOffer.KeySkills}");
        prompt.AppendLine($"**Niveau d'expérience requis:** {jobOffer.ExperienceLevel}");
        prompt.AppendLine($"**Type de contrat:** {jobOffer.JobType}");
        prompt.AppendLine($"**Département:** {jobOffer.Department}");

        prompt.AppendLine("\n**Candidatures:**");
        for (int i = 0; i < applications.Count; i++)
        {
            var app = applications[i];
            prompt.AppendLine($"### Candidat {i + 1}: {app.CandidateName}"); // Supprimé l'ID

            if (!string.IsNullOrEmpty(app.ResumeText))
            {
                var resumePreview = app.ResumeText.Length > 1000
                    ? app.ResumeText.Substring(0, 1000) + "..."
                    : app.ResumeText;
                prompt.AppendLine($"**CV:** {resumePreview}");
            }

            if (!string.IsNullOrEmpty(app.CoverLetter))
            {
                var coverLetterPreview = app.CoverLetter.Length > 500
                    ? app.CoverLetter.Substring(0, 500) + "..."
                    : app.CoverLetter;
                prompt.AppendLine($"**Lettre de motivation:** {coverLetterPreview}");
            }
        }

        prompt.AppendLine(@"
**Consignes IMPORTANTES:**
1. Classez les candidats par ordre de pertinence (1 = meilleur)
2. Attribuez un score de 0 à 100
3. Justifiez brièvement chaque choix en français
4. Retournez uniquement les 5 meilleurs candidats (ou moins s'il y en a moins)
5. IMPÉRATIF: Retournez UNIQUEMENT un tableau JSON valide, sans texte avant ou après
6. N'incluez JAMAIS les IDs dans la justification, utilisez seulement les noms

Format de réponse OBLIGATOIRE (JSON pur):
[
  {
    ""candidateName"": ""Nom du candidat"",
    ""score"": 85,
    ""justification"": ""Expérience solide en audit et excellentes compétences analytiques""
  }
]");

        return prompt.ToString();
    }

    private List<CandidateRecommendation> ParseRecommendations(string? responseText, List<ApplicationData> applications)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return CreateFallbackRecommendations(applications);
        }

        try
        {
            // Nettoyer la réponse pour extraire seulement le JSON
            var cleanedJson = ExtractJsonFromResponse(responseText);

            if (string.IsNullOrWhiteSpace(cleanedJson))
            {
                return CreateFallbackRecommendations(applications);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var recommendations = JsonSerializer.Deserialize<List<dynamic>>(cleanedJson, options);

            if (recommendations == null || !recommendations.Any())
            {
                return CreateFallbackRecommendations(applications);
            }

            var result = new List<CandidateRecommendation>();

            foreach (var item in recommendations.Take(5))
            {
                try
                {
                    var jsonElement = (JsonElement)item;
                    var candidateName = jsonElement.GetProperty("candidateName").GetString();
                    var score = jsonElement.GetProperty("score").GetDouble();
                    var justification = jsonElement.GetProperty("justification").GetString();

                    // Trouver l'application correspondante par nom
                    var application = applications.FirstOrDefault(a => 
                        string.Equals(a.CandidateName, candidateName, StringComparison.OrdinalIgnoreCase));

                    if (application != null)
                    {
                        result.Add(new CandidateRecommendation
                        {
                            ApplicationId = application.ApplicationId,
                            CandidateName = candidateName,
                            Score = score,
                            Justification = justification ?? "Candidat recommandé par l'IA"
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing recommendation item: {ex.Message}");
                    continue;
                }
            }

            return result.Any() ? result : CreateFallbackRecommendations(applications);
        }
        catch (JsonException ex)
        {
            // Log l'erreur pour debug
            System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Response text: {responseText}");

            return CreateFallbackRecommendations(applications);
        }
        catch (Exception ex)
        {
            // Log l'erreur pour debug
            System.Diagnostics.Debug.WriteLine($"General parsing error: {ex.Message}");

            return CreateFallbackRecommendations(applications);
        }
    }

    private string ExtractJsonFromResponse(string responseText)
    {
        try
        {
            // Rechercher le premier '[' et le dernier ']' pour extraire le JSON
            var startIndex = responseText.IndexOf('[');
            var endIndex = responseText.LastIndexOf(']');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                return responseText.Substring(startIndex, endIndex - startIndex + 1);
            }

            // Si pas de crochets, chercher des accolades pour un objet JSON
            startIndex = responseText.IndexOf('{');
            endIndex = responseText.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                var jsonContent = responseText.Substring(startIndex, endIndex - startIndex + 1);
                // Envelopper dans un tableau si c'est un objet unique
                return $"[{jsonContent}]";
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private List<CandidateRecommendation> CreateFallbackRecommendations(List<ApplicationData> applications)
    {
        // Créer des recommandations de base en cas d'échec de l'IA
        return applications
            .Take(Math.Min(5, applications.Count))
            .Select((app, index) => new CandidateRecommendation
            {
                ApplicationId = app.ApplicationId,
                CandidateName = app.CandidateName,
                Score = 70.0 - (index * 5), // Score décroissant
                Justification = "Candidature évaluée selon l'ordre de soumission en attendant l'analyse IA."
            })
            .ToList();
    }

    public class CandidateRecommendation
    {
        public Guid ApplicationId { get; set; }
        public double Score { get; set; }
        public string Justification { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
    }

    public class ApplicationData
    {
        public Guid ApplicationId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string? ResumeText { get; set; }
        public string? CoverLetter { get; set; }
    }

    public class GeminiResponse
    {
        public Candidate[] candidates { get; set; } = Array.Empty<Candidate>();
    }

    public class Candidate
    {
        public Content content { get; set; } = new Content();
    }

    public class Content
    {
        public Part[] parts { get; set; } = Array.Empty<Part>();
    }

    public class Part
    {
        public string text { get; set; } = string.Empty;
    }
}