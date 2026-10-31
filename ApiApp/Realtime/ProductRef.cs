namespace ApiApp.Realtime;

/// <summary>Товар у повідомленні: id, категорія й назви обома мовами.</summary>
public sealed record ProductRef(int Id, int CategoryId, string NameUk, string NameEn);
