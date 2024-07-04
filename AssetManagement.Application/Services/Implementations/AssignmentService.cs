using AssetManagement.Application.Common.Constants;
using AssetManagement.Application.Common.Credential;
using AssetManagement.Application.Common.ExpressionBuilder;
using AssetManagement.Application.Services.Interfaces;
using AssetManagement.Contracts.Dtos.AssignmentDtos.Requests;
using AssetManagement.Contracts.Dtos.AssignmentDtos.Responses;
using AssetManagement.Contracts.Dtos.PaginationDtos;
using AssetManagement.Contracts.Enums;
using AssetManagement.Data.Interfaces;
using AssetManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AssetManagement.Domain.Enums;
using AssetManagement.Domain.Exceptions;

namespace AssetManagement.Application.Services.Implementations {
    public class AssignmentService : IAssignmentService {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly UserManager<AppUser> _userManager;

        public AssignmentService(ICurrentUser currentUser, IUnitOfWork unitOfWork, UserManager<AppUser> userManager) {
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Guid> CreateAssignmentAsync(AssignmentCreationRequest request)
        {
            var userAssignedTo = await GetUserAssignedById(request.UserId);

            var asset = await GetAssetById(request.AssetId);

            var newAssignment = new Assignment
            {
                Id = Guid.NewGuid(),
                AssetId = asset.Id,
                AssignedById = _currentUser.UserId,
                AssignedToId = userAssignedTo.Id,
                Note = request.Note,
                AssignedDate = request.AssignedDate,
                State = AssignmentState.WaitingForAcceptance,
            };

            _unitOfWork.AssignmentRepository.Add(newAssignment);

            //Update state of asset
            asset.State = AssetState.NotAvailable;
            _unitOfWork.AssetRepository.Update(asset);

            await _unitOfWork.SaveChangesAsync();
            return newAssignment.Id;
        }

        private async Task<AppUser> GetUserAssignedById(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User to assigned is not found!");
            }
            else if (user.IsDisabled)
            {
                throw new BadRequestException("User to assigned is disabled!");
            }
            return user;
        }

        private async Task<Asset> GetAssetById(Guid assetId)
        {
            var asset = await _unitOfWork.AssetRepository.FindOne(a => a.Id.Equals(assetId));
            if (asset == null)
            {
                throw new NotFoundException("Asset to assigned is not found!");
            }
            else if (AssetState.Available != asset.State)
            {
                throw new BadRequestException("Asset is not available to assigned!");
            }
            return asset;
        }
        public async Task<PagingDto<FilterAssignmentResponse>> FilterAssignmentAsync(FilterAssignmentRequest filter) {
            var currentUser = await _userManager.Users
                .Where(u => _currentUser.UserId.Equals(u.Id))
                .Select(u => new AppUser()
                {
                    Id = u.Id,
                    IsDisabled = u.IsDisabled,
                    Location = u.Location,
                })
                .FirstOrDefaultAsync()
                .ContinueWith(t => t.Result ?? throw new UnauthorizedAccessException(ErrorStrings.USER_NOT_LOGIN));

            if (currentUser.IsDisabled) {
                throw new UnauthorizedAccessException(ErrorStrings.USER_IS_DISABLED);
            }

            var queryable = _unitOfWork.AssignmentRepository.GetQueryableSet();
            //set default page size
            if (!filter.PageNumber.HasValue || !filter.PageSize.HasValue
                || filter.PageNumber.Value <= 0 || filter.PageSize.Value <= 0) {
                filter.PageNumber = 1;
                filter.PageSize = 5;
            }
            var filterSpecification = GetSpecification(filter, currentUser);
            var filteredQueryable = queryable.Where(filterSpecification);

            var orderBy = GetOrderBy(filter);
            var finalQueryable = orderBy(filteredQueryable);

            var result = await finalQueryable
            .AsNoTracking()
            .Skip((filter.PageNumber.Value - 1) * filter.PageSize.Value)
            .Take(filter.PageSize.Value)
            .Select(a => new FilterAssignmentResponse()
            {
                Id = a.Id,
                AssetCode = a.Asset.AssetCode,
                AssetName = a.Asset.Name,
                AssignedBy = a.AssignedByUser.UserName,
                AssignedTo = a.AssignedToUser.UserName,
                AssignedDate = a.AssignedDate,
                State = a.State,
            }).ToListAsync();

            int totalRecord = await filteredQueryable.CountAsync();

            return new PagingDto<FilterAssignmentResponse>()
            {
                CurrentPage = filter.PageNumber.Value,
                TotalItemCount = totalRecord,
                PageSize = filter.PageSize.Value,
                Data = result
            };
        }

        #region private methods

        private Func<IQueryable<Assignment>, IOrderedQueryable<Assignment>> GetOrderBy(FilterAssignmentRequest filter) {
            return filter switch
            {
                { SortAssetCode: SortOption.Asc } => q => q.OrderBy(a => a.Asset!.AssetCode),
                { SortAssetCode: SortOption.Desc } => q => q.OrderByDescending(a => a.Asset!.AssetCode),
                { SortAssetName: SortOption.Asc } => q => q.OrderBy(a => a.Asset!.Name),
                { SortAssetName: SortOption.Desc } => q => q.OrderByDescending(a => a.Asset!.Name),
                { SortAssignedTo: SortOption.Asc } => q => q.OrderBy(a => a.AssignedToUser!.UserName),
                { SortAssignedTo: SortOption.Desc } => q => q.OrderByDescending(a => a.AssignedToUser!.UserName),
                { SortAssignedBy: SortOption.Asc } => q => q.OrderBy(a => a.AssignedByUser!.UserName),
                { SortAssignedBy: SortOption.Desc } => q => q.OrderByDescending(a => a.AssignedByUser!.UserName),
                { SortAssignedDate: SortOption.Asc } => q => q.OrderBy(a => a.AssignedDate),
                { SortAssignedDate: SortOption.Desc } => q => q.OrderByDescending(a => a.AssignedDate),
                { SortState: SortOption.Asc } => q => q.OrderBy(a => a.State),
                { SortState: SortOption.Desc } => q => q.OrderByDescending(a => a.State),
                { SortLastUpdate: SortOption.Asc } => q => q.OrderBy(a => a.LastUpdated),
                { SortLastUpdate: SortOption.Desc } => q => q.OrderByDescending(a => a.LastUpdated),
                _ => q => q.OrderByDescending(a => a.AssignedDate)
            };
        }
        private Expression<Func<Assignment, bool>> GetSpecification(FilterAssignmentRequest filter, AppUser currentUser) {
            Expression<Func<Assignment, bool>> filterSpecification = PredicateBuilder.True<Assignment>();
            filterSpecification = filterSpecification.And(a => a.Asset != null && a.Asset!.Location == currentUser.Location);

            if (!string.IsNullOrWhiteSpace(filter.Search)) {
                filterSpecification = filterSpecification.And(
                    a => (a.Asset != null && a.Asset.Name != null && a.Asset.Name.ToLower().Contains(filter.Search.Trim().ToLower()))
                    || (a.Asset != null && a.Asset.AssetCode != null && a.Asset.AssetCode.ToLower().Contains(filter.Search.Trim().ToLower()))
                    || (a.AssignedToUser != null && a.AssignedToUser.UserName != null && a.AssignedToUser.UserName.ToLower().Contains(filter.Search.Trim().ToLower())));
            }

            if (filter.States != null && filter.States.Length > 0) {
                filterSpecification = filterSpecification.And(a => filter.States.Any(s => s == a.State));
            }

            if (filter.AssignedDate.HasValue) {
                filterSpecification = filterSpecification.And(a => a.AssignedDate.Date == filter.AssignedDate.Value.Date);
            }
            return filterSpecification;
        }

        #endregion
    }
}
