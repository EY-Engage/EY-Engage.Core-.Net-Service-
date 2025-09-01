

namespace EYEngage.Core.Application.Dto.JobDto;



    public class ScheduleInterviewRequest
    {
        public Guid ApplicationId { get; set; }
        public DateTime InterviewDate { get; set; }
        public string Location { get; set; }
    }
