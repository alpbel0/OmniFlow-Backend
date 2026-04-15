namespace OmniFlow.Application.DTOs.Media;

public class UploadMediaResponse
{
	public List<UploadedMediaFileResponse> Files { get; set; } = new();
}
