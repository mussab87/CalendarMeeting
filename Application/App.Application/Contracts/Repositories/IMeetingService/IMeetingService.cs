namespace App.Application.Contracts.Repositories.IMeetingService
{ }
public interface IMeetingService
{
    Task<MeetingDto> GetMeetingByIdAsync(int id);
    Task<IEnumerable<MeetingDto>> GetAllMeetingsAsync();
    Task<IEnumerable<MeetingDto>> GetMeetingsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<MeetingDto>> GetUserMeetingsAsync(string userId);
    Task<IEnumerable<MeetingDto>> GetTodayMeetingsAsync(string userId);
    Task<MeetingDto> CreateMeetingAsync(CreateMeetingDto createDto, string organizerId);
    Task<MeetingDto> UpdateMeetingAsync(int id, UpdateMeetingDto updateDto);
    Task<bool> DeleteMeetingAsync(int id);
    Task<bool> InviteParticipantsAsync(int meetingId, List<string> userIds);
    Task<bool> RespondToInvitationAsync(int meetingId, string userId, int responseStatus);
    Task<ParticipantDto> GetMeetingParticipantAsync(int meetingId, string userId);
    Task<bool> UpdateParticipantResponseAsync(int meetingId, string userId, ResponseStatusEnum responseStatus, string declinedReason);
    Task<IEnumerable<object>> GetAcceptedParticipantsAsync(int meetingId);
    Task<IEnumerable<MeetingDto>> GetMeetingsByDepartmentAsync(int departmentId);
}

