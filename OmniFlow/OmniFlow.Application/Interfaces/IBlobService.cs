namespace OmniFlow.Application.Interfaces;

public interface IBlobService
{
	/// <summary>
	/// Dosyayı blob depoya yükler; herkese açık okuma URL'si döner.
	/// </summary>
	/// <param name="stream">Yüklenecek içerik akışı (dispose çağıran tarafta).</param>
	/// <param name="contentType">MIME türü (ör. image/jpeg).</param>
	/// <param name="originalFileName">Uzantı için kullanılır; null olabilir.</param>
	Task<string> UploadAsync(
		Stream stream,
		string contentType,
		string? originalFileName,
		CancellationToken cancellationToken = default);
}
