using System.ComponentModel;

namespace StreamFileApi.Model.Responses;

public class ApiError
{
    [DefaultValue(400)]
    public int StatusCode { get; set; }

    public string? Message { get; set; }
}