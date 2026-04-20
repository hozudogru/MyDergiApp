using Microsoft.AspNetCore.Hosting;

namespace MyDergiApp.Services
{
    public class EmailTemplateService
    {
        private readonly IWebHostEnvironment _env;

        public EmailTemplateService(IWebHostEnvironment env)
        {
            _env = env;
        }

        private async Task<string> LoadTemplateAsync(string fileName)
        {
            var path = Path.Combine(_env.ContentRootPath, "Templates", "Emails", fileName);

            if (!File.Exists(path))
                throw new Exception($"Template bulunamadı: {path}");

            return await File.ReadAllTextAsync(path);
        }

        public async Task<string> BuildEditorDecisionAsync(
            string authorName,
            string submissionTitle,
            string decision,
            string decisionNote)
        {
            var html = await LoadTemplateAsync("EditorDecision.html");

            html = html.Replace("{{AuthorName}}", authorName);
            html = html.Replace("{{SubmissionTitle}}", submissionTitle);
            html = html.Replace("{{Decision}}", decision);
            html = html.Replace("{{DecisionNote}}", decisionNote);

            return html;
        }
        public async Task<string> BuildReviewerAssignmentAsync(
            string reviewerName,
            string submissionTitle)
        {
            var html = await LoadTemplateAsync("ReviewerAssignment.html");

            html = html.Replace("{{ReviewerName}}", reviewerName);
            html = html.Replace("{{SubmissionTitle}}", submissionTitle);

            return html;
        }
    }
}