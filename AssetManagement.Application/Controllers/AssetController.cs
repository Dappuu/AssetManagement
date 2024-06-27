﻿using AssetManagement.Application.Common;
using AssetManagement.Application.Services.Interfaces;
using AssetManagement.Contracts.Dtos.AssetDtos.Requests;
using AssetManagement.Contracts.Dtos.AssetDtos.Responses;
using AssetManagement.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Application.Controllers;
[Route("api/asset")]
[ApiController]
public class AssetController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AssetController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet("getAssetById")]
    [Authorize(Roles = $"{RoleConstant.AdminRole}")]
    public async Task<ActionResult<BaseResult<AssetDetailsResponse>>> GetAssetById([FromQuery] AssetDetailsRequest request)
    {
        var data = await _assetService.GetAssetByIdAsync(request);
        var result = new BaseResult<AssetDetailsResponse>()
        {
            IsSuccess = true,
            Error = null,
            Result = data
        };
        return Ok(result);
    }

    [HttpPut("updateAssetById")]
    [Authorize(Roles = $"{RoleConstant.AdminRole}")]
    public async Task<ActionResult<BaseResult<object>>> UpdateAssetById([FromQuery] AssetUpdateRequest request)
    {
        await _assetService.UpdateAssetAsync(request);
        var result = new BaseResult<object>()
        {
            IsSuccess = true,
            Error = null,
            Result = null,
        };
        return Ok(result);
    }
}
