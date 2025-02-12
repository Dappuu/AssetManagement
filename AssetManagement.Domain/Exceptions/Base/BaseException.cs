﻿namespace AssetManagement.Domain.Exceptions.Base;
public class BaseException : Exception
{
    public int StatusCode { get; set; }
    public string? Title { get; set; }

    public string? Type { get; set; }

    public BaseException()
    {
    }

    public BaseException(string? message) : base(message)
    {
    }

    public BaseException(string? message, int statusCode, string? title) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }
}
