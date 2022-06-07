using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

/*
- Bir tren içinde birden fazla vagon bulunabilir

- Her vagonun farklý kiþi kapasitesi olabilir

- Online rezervasyonlarda, bir vagonun doluluk kapasitesi %70'i geçmemelidir. Yani vagon kapasitesi 100 ise ve 70 koltuk dolu ise, o vagona rezervasyon yapýlamaz.

- Bir rezervasyon isteði içinde birden fazla kiþi olabilir.

- Rezervasyon isteði yapýlýrken, kiþilerin farklý vagonlara yerleþip yerleþtirilemeyeceði belirtilir. Bazý rezervasyonlarda tüm yolcularýn ayný vagonda olmasý istenilirken, bazýlarýnda farklý vagonlar da kabul edilebilir.

- Rezervasyon yapýlabilir durumdaysa, API hangi vagonlara kaçar kiþi yerleþeceði bilgisini dönecektir.
*/

/*
{
    "Tren":
    {
        "Ad":"Baþkent Ekspres",
        "Vagonlar":
        [
            {"Ad":"Vagon 1", "Kapasite":100, "DoluKoltukAdet":50},
            {"Ad":"Vagon 2", "Kapasite":90, "DoluKoltukAdet":80},
            {"Ad":"Vagon 3", "Kapasite":80, "DoluKoltukAdet":80}
        ]
    },
    "RezervasyonYapilacakKisiSayisi":3,
    "KisilerFarkliVagonlaraYerlestirilebilir":true
}
 */

app.MapPost("/rezervasyon", (RezervasyonRequest rezervasyonRequest) =>
{
    var rezervasyonYapilacakKisiSayisi = rezervasyonRequest.RezervasyonYapilacakKisiSayisi;
    var rezervasyonResponse = new RezervasyonResponse();
    var yerlesimAyrinti = rezervasyonResponse.YerlesimAyrinti;
    var vagonlar = Array.Empty<Vagon>();
    for (vagonlar = rezervasyonRequest.rezerveEdilebilirVagonlar(); rezervasyonYapilacakKisiSayisi > 0 && vagonlar.Length > 0; vagonlar = rezervasyonRequest.rezerveEdilebilirVagonlar())
    {
        foreach (var vagon in vagonlar)
        {
            var ayrinti = Array.Find(yerlesimAyrinti, ayrinti => ayrinti.VagonAdi == vagon.Ad);
            if (ayrinti == null)
            {
                ayrinti = new YerlesimAyrinti() { VagonAdi = vagon.Ad };
                Array.Resize(ref yerlesimAyrinti, yerlesimAyrinti.Length + 1);
                yerlesimAyrinti[yerlesimAyrinti.Length - 1] = ayrinti;
            }
            if (rezervasyonRequest.KisilerFarkliVagonlaraYerlestirilebilir == true)
            {
                vagon.DoluKoltukAdet++;
                ayrinti.KisiSayisi++;
                rezervasyonYapilacakKisiSayisi--;

                if (rezervasyonYapilacakKisiSayisi <= 0)
                {
                    break;
                }
            }
            else
            {
                ayrinti.KisiSayisi = rezervasyonYapilacakKisiSayisi;
                rezervasyonYapilacakKisiSayisi = 0;
                break;
            }
        }
    }
    
    if (vagonlar.Length <= 0)
    {
        rezervasyonResponse.RezervasyonYapilabilir = false;
    }

    rezervasyonResponse.YerlesimAyrinti = yerlesimAyrinti;

    if (!(rezervasyonYapilacakKisiSayisi <= 0))
    {
        rezervasyonResponse.YerlesimAyrinti = Array.Empty<YerlesimAyrinti>();
    }
    return rezervasyonResponse;
})
.WithName("PostRezervasyon");

app.Run();

internal record RezervasyonRequest
{
    [Required]
    public Tren? Tren { get; set; }
    [Required]
    public int? RezervasyonYapilacakKisiSayisi { get; set; }
    [Required]
    public bool? KisilerFarkliVagonlaraYerlestirilebilir { get; set; }

    public Vagon[]? rezerveEdilebilirVagonlar()
    {
        if (KisilerFarkliVagonlaraYerlestirilebilir == true)
        {
            return Array.FindAll(Tren.Vagonlar, vagon => !((((double)vagon.DoluKoltukAdet / (double)vagon.Kapasite) * 100) >= 70));
        }
        return Array.FindAll(Tren.Vagonlar, vagon => !((((double)(vagon.DoluKoltukAdet + RezervasyonYapilacakKisiSayisi) / (double)vagon.Kapasite) * 100) >= 70));
    }
}

internal record Tren
{
    [Required]
    public string? Ad { get; set; }

    [Required]
    public Vagon[]? Vagonlar { get; set; } = Array.Empty<Vagon>();
}

internal record Vagon
{
    [Required]
    public string? Ad { get; set; }
    [Required]
    public int? Kapasite { get; set; }
    [Required]
    public int? DoluKoltukAdet { get; set; }
}

internal record RezervasyonResponse
{
    public bool? RezervasyonYapilabilir { get; set; } = true;
    public YerlesimAyrinti[]? YerlesimAyrinti { get; set; } = Array.Empty<YerlesimAyrinti>();
}

internal record YerlesimAyrinti
{
    public string? VagonAdi { get; set; } = "";
    public int? KisiSayisi { get; set; } = 0;
}