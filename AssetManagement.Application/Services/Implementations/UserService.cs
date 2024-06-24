﻿using AssetManagement.Application.Common.Constants;
using AssetManagement.Application.Common.Credential;
using AssetManagement.Application.Services.Interfaces;
using AssetManagement.Contracts.Dtos.PaginationDtos;
using AssetManagement.Contracts.Dtos.UserDtos.Requests;
using AssetManagement.Contracts.Dtos.UserDtos.Responses;
using AssetManagement.Contracts.Enums;
using AssetManagement.Domain.Entities;
using AssetManagement.Domain.Exceptions;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AssetManagement.Application.Services.Implementations;
public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UserService> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public UserService(UserManager<AppUser> userManager, ILogger<UserService> logger, ICurrentUser currentUser, IMapper mapper)
    {
        _userManager = userManager;
        _logger = logger;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<PagingDto<FilterUserResponse>> FilterUserAsync(FilterUserRequest request)
    {
        var queryable = _userManager.Users;

        var currentUser = await _userManager.FindByIdAsync(_currentUser.UserId.ToString())
            .ContinueWith(t => t.Result ?? throw new UnauthorizedAccessException("User do not login"));

        //set default page size
        if (!request.PageNumber.HasValue || !request.PageSize.HasValue
            || request.PageNumber.Value <= 0 || request.PageSize.Value <= 0)
        {
            request.PageNumber = 1;
            request.PageSize = 5;
        }

        try
        {
            var orderBy = GetOrderByExpression(request);

            Expression<Func<AppUser, bool>> filterSpecification = u => (string.IsNullOrEmpty(request.Name) || (u.FirstName + u.LastName).Contains(request.Name) || u.StaffCode.Contains(request.Name))
            && (request.Types == null || !request.Types.Any() || request.Types.Contains(u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault())
            && u.Location == currentUser.Location
            && !u.IsDisabled);

            var totalRecord = await queryable.Where(filterSpecification).CountAsync();

            queryable = orderBy(queryable);

            var result = await queryable
                .AsNoTracking()
                .Where(filterSpecification)
                .Skip((request.PageNumber.Value - 1) * request.PageSize.Value)
                .Take(request.PageSize.Value)
                .Select(u => new FilterUserResponse()
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}",
                    JoinedDate = u.JoinedDate,
                    StaffCode = u.StaffCode,
                    Username = u.UserName,
                    Types = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                }).ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalRecord / request.PageSize.Value);
            return new PagingDto<FilterUserResponse>()
            {
                CurrentPage = request.PageNumber.Value,
                TotalItemCount = totalRecord,
                PageSize = request.PageSize.Value,
                Data = result
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Error when execute {} method.\nDate: {}.\nDetail: {}", nameof(this.FilterUserAsync),
                DateTime.UtcNow, e.Message);
            throw new Exception($"Error when execute {nameof(this.FilterUserAsync)} method");
        }
    }

    public async Task<DisableUserResponse> DisableUserAsync(DisableUserRequest request)
    {
        try
        {
            var userToBeDisabled = await _userManager.FindByIdAsync(request.UserId) ?? throw new NotFoundException(ErrorStrings.USER_NOT_FOUND);
            userToBeDisabled.IsDisabled = true;
            var result = await _userManager.UpdateAsync(userToBeDisabled);
            if (result.Succeeded)
            {
                return new DisableUserResponse();
            }
            throw new Exception(string.Join(". ", result.Errors.Select(p => p.Description)));
        }
        catch (Exception e)
        {
            _logger.LogError("Error when execute {} method.\nDate: {}.\nDetail: {}", nameof(this.DisableUserAsync),
                DateTime.UtcNow, e.Message);
            throw new Exception($"Error when execute {nameof(this.DisableUserAsync)} method");
        }
    }

    private Func<IQueryable<AppUser>, IOrderedQueryable<AppUser>> GetOrderByExpression(FilterUserRequest filter)
    {
        Func<IQueryable<AppUser>, IOrderedQueryable<AppUser>> orderBy = q =>
                q.OrderByDescending(u => u.JoinedDate);

        if (filter.SortStaffCode != null && filter.SortStaffCode.Equals(SortOption.Asc))
        {
            orderBy = q => q.OrderBy(u => u.StaffCode);
        }
        else if (filter.SortStaffCode != null && filter.SortStaffCode.Equals(SortOption.Desc))
        {
            orderBy = q => q.OrderByDescending(u => u.StaffCode);
        }
        else if (filter.SortFullName != null && filter.SortFullName.Equals(SortOption.Asc))
        {
            orderBy = q => q.OrderBy(u => u.FirstName + u.LastName);
        }
        else if (filter.SortFullName != null && filter.SortFullName.Equals(SortOption.Desc))
        {
            orderBy = q => q.OrderByDescending(u => u.FirstName + u.LastName);
        }
        else if (filter.SortJoinedDate != null && filter.SortJoinedDate.Equals(SortOption.Asc))
        {
            orderBy = q => q.OrderBy(u => u.JoinedDate);
        }
        else if (filter.SortJoinedDate != null && filter.SortJoinedDate.Equals(SortOption.Desc))
        {
            orderBy = q => q.OrderByDescending(u => u.JoinedDate);
        }
        else if (filter.SortType != null && filter.SortType.Equals(SortOption.Asc))
        {
            orderBy = q => q.OrderBy(u => u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault());
        }
        else if (filter.SortType != null && filter.SortType.Equals(SortOption.Desc))
        {
            orderBy = q => q.OrderByDescending(u => u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault());
        }
        else if (filter.SortLastUpdate != null && filter.SortLastUpdate.Equals(SortOption.Asc))
        {
            orderBy = q => q.OrderBy(u => u.LastUpdatedDateTime);
        }
        else if (filter.SortLastUpdate != null && filter.SortLastUpdate.Equals(SortOption.Desc))
        {
            orderBy = q => q.OrderByDescending(u => u.LastUpdatedDateTime);
        }
        return orderBy;
    }

    public async Task<UserInfoResponse> GetUserById(Guid id)
    {
        var queryable = _userManager.Users;
        var appUser = await queryable.Where(q => q.Id == id).Include(q => q.UserRoles).ThenInclude(q => q.Role).FirstOrDefaultAsync();
        if (appUser == null)
        {
            throw new NotFoundException("User can not found");
        }
        var result = _mapper.Map<UserInfoResponse>(appUser);
        return result;
    }
}

