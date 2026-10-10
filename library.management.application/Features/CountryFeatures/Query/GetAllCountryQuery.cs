using System.Text.Json;
using library.management.application.Interfaces.Commons;
using library.management.application.Interfaces.Repositories;
using library.management.application.Shared.Mediator;
using library.management.domain.Enums;
using library.management.domain.Models.Entities;
using library.management.domain.Models.ViewModels.CountryVM;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace library.management.application.Features.CountryFeatures.Query
{
    public sealed class GetAllCountryQuery: IRequest<Result<List<CountryVM>>>
    {
    }

    public sealed class GetAllCountryQueryHandler: IRequestHandler<GetAllCountryQuery, Result<List<CountryVM>>>
    {
        private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ICountryRepository _countryRepository;
        private readonly ILogger<GetAllCountryQueryHandler> _logger;
        private readonly IAuditLogger _auditLogger;

        public GetAllCountryQueryHandler(ICountryRepository countryRepository, ILogger<GetAllCountryQueryHandler> logger, IAuditLogger auditLogger)
        {
            _countryRepository = countryRepository;
            _logger = logger;
            _auditLogger = auditLogger;
        }

        public async Task<Result<List<CountryVM>>> Handle(GetAllCountryQuery query, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Retrieving all active countries");

            var countryEntities = await _countryRepository.GetAsync(country => country.IsActive, asNoTracking: true, cancellationToken: cancellationToken);

            var countries = countryEntities.OrderBy(country => country.CountryFullname, StringComparer.OrdinalIgnoreCase)
                .Select(country => new CountryVM
                {
                    CountryId = country.Id,
                    CountryFullname = country.CountryFullname,
                    CountryShortname = country.CountryShortname,
                    CountryDescription = country.CountryDescription,
                    CountryCode2 = country.CountryCode2,
                    CountryCode3 = country.CountryCode3,
                    CountryIsoCode = country.CountryIsoCode
                }).ToList();

            var auditMetadata = JsonSerializer.Serialize(
                new
                {
                    Operation = "GetAll",
                    ResultCount = countries.Count,
                    Filters = new
                    {
                        IsActive = true,
                        IsDeleted = false
                    }
                },
                AuditJsonOptions);

            await _auditLogger.LogAsync(
                action: AuditActionEnum.View,
                entityName: nameof(Country),
                entityId: "LIST",
                metadata: auditMetadata,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Retrieved {CountryCount} active countries", countries.Count);

            var result = Result.Ok(countries);

            if (countries.Count == 0)
            {
                result.WithSuccess("No active countries were found.");
            }

            return result;
        }
    }
}