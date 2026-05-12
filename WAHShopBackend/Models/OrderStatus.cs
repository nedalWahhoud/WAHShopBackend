namespace WAHShopBackend.Models
{
    public class OrderStatus
    {
        public int Id { get; set; }
        public string Status_de { get; set; } = string.Empty;
        public string Status_ar { get; set; } = string.Empty;
        public string? StatusPresent_de { get; set; } = string.Empty;
        public string? StatusPresent_ar { get; set; } = string.Empty;
    }

    public enum OrderStatusEnum
    {
        Eingegangen = 13,
        InBearbeitung = 14,
        Versendet = 15,
        Zugestellt = 16,
        Storniert = 17,
        Abgelehnt = 18,
        Bezahlt = 19,
        RückgabeAngefordert = 20,
        RückgabeAbgeschlossen = 21,
        Rückerstattet = 22,
        ZustellungFehlgeschlagen = 23,
        erfolgreichAbgeschlossen = 24,
        Abgeholt = 25,
    }
}
