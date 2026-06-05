using MyDergiApp.Models;

namespace MyDergiApp.Helpers
{
    public static class StatusDisplayHelper
    {
        public static string GetSubmissionStatusText(SubmissionStatus status)
        {
            return status switch
            {
                SubmissionStatus.Taslak => "Taslak",
                SubmissionStatus.Gonderildi => "Gönderildi",
                SubmissionStatus.OnKontrolBekliyor => "Ön Kontrol Bekliyor",
                SubmissionStatus.YazaraIadeEdildi => "Yazara İade Edildi",
                SubmissionStatus.AlanEditoruneYonlendirildi => "Alan Editörüne Yönlendirildi",
                SubmissionStatus.AlanEditorunde => "Alan Editöründe",
                SubmissionStatus.HakemAtamasiBekliyor => "Hakem Ataması Bekliyor",
                SubmissionStatus.HakemDegerlendirmesinde => "Hakem Değerlendirmesinde",
                SubmissionStatus.RevizyonIstendi => "Revizyon İstendi",
                SubmissionStatus.RevizyonYuklendi => "Revizyon Yüklendi",
                SubmissionStatus.KabulEdildi => "Kabul Edildi",
                SubmissionStatus.Reddedildi => "Reddedildi",
                SubmissionStatus.GeriCekildi => "Geri Çekildi",
                _ => status.ToString()
            };
        }

        public static string GetSubmissionStatusBadgeClass(SubmissionStatus status)
        {
            return status switch
            {
                SubmissionStatus.Taslak => "bg-secondary",
                SubmissionStatus.Gonderildi => "bg-primary",
                SubmissionStatus.OnKontrolBekliyor => "bg-warning text-dark",
                SubmissionStatus.YazaraIadeEdildi => "bg-warning text-dark",
                SubmissionStatus.AlanEditoruneYonlendirildi => "bg-info text-dark",
                SubmissionStatus.AlanEditorunde => "bg-info text-dark",
                SubmissionStatus.HakemAtamasiBekliyor => "bg-dark",
                SubmissionStatus.HakemDegerlendirmesinde => "bg-primary",
                SubmissionStatus.RevizyonIstendi => "bg-warning text-dark",
                SubmissionStatus.RevizyonYuklendi => "bg-info text-dark",
                SubmissionStatus.KabulEdildi => "bg-success",
                SubmissionStatus.Reddedildi => "bg-danger",
                SubmissionStatus.GeriCekildi => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        public static string GetReviewerAssignmentBadgeClass(ReviewerAssignmentStatus status)
        {
            return status switch
            {
                ReviewerAssignmentStatus.Assigned => "bg-warning text-dark",
                ReviewerAssignmentStatus.InReview => "bg-primary",
                ReviewerAssignmentStatus.Completed => "bg-success",
                ReviewerAssignmentStatus.Declined => "bg-danger",
                ReviewerAssignmentStatus.Cancelled => "bg-secondary",
                ReviewerAssignmentStatus.Overdue => "bg-danger",
                _ => "bg-secondary"
            };
        }

        public static string GetReviewerAssignmentStatusText(ReviewerAssignmentStatus status)
        {
            return status switch
            {
                ReviewerAssignmentStatus.Assigned => "Atandı",
                ReviewerAssignmentStatus.InReview => "Değerlendirmede",
                ReviewerAssignmentStatus.Completed => "Tamamlandı",
                ReviewerAssignmentStatus.Declined => "Reddedildi",
                ReviewerAssignmentStatus.Cancelled => "İptal Edildi",
                ReviewerAssignmentStatus.Overdue => "Gecikti",
                _ => status.ToString()
            };
        }

        public static string GetFileTypeText(string? fileType)
        {
            return fileType switch
            {
                "MakaleDosyasi" => "Ana Makale Dosyası",
                "RevizyonDosyasi" => "Yazar Revizyon Dosyası",
                "HakemEkDosyasi" => "Hakem Ek Dosyası",
                "KapakYazisi" => "Kapak Yazısı",
                _ => string.IsNullOrWhiteSpace(fileType) ? "Dosya" : fileType
            };
        }

        public static string GetFileTypeBadgeClass(string? fileType)
        {
            return fileType switch
            {
                "MakaleDosyasi" => "bg-secondary",
                "RevizyonDosyasi" => "bg-info text-dark",
                "HakemEkDosyasi" => "bg-primary",
                "KapakYazisi" => "bg-light text-dark border",
                _ => "bg-secondary"
            };
        }
    }
}