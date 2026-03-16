namespace OmniFlow.Application.Wrappers;

public class PagedResponse<T>
{
	public IReadOnlyList<T> Data { get; init; }
	public int PageNumber { get; init; }
	public int PageSize { get; init; }
	public int TotalCount { get; init; }

	public PagedResponse(IReadOnlyList<T> data, int pageNumber, int pageSize, int totalCount)
	{
		Data = data;
		PageNumber = pageNumber;
		PageSize = pageSize;
		TotalCount = totalCount;
	}
}
