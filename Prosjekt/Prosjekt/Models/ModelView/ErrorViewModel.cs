namespace Prosjekt.Models.ModelView
{
    public class ErrorViewModel
    {
        public string? RequestId { get; init; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}