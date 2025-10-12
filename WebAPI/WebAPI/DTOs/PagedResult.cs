using System.Collections.Generic;

namespace WebAPI.DTOs
{
	public sealed class PagedResult<T>
	{
		public List<T> Items { get; set; } = new List<T>();
		public int Total { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
	}
}


