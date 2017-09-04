using MapIndex.Infrastructure.Configuration;
using MapIndex.MapModels.DFS;
using MapIndex.MapModels.MA;
using MapIndex.MapModels.Portfolio;
using MapIndex.Models;
using MapIndex.Models.DFS;
using MapIndex.Models.MA;
using MapIndex.Models.Portfolio;
using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Web.Http;

namespace MapIndex.Controllers
{
    public class MapDataController : ApiController
    {
        private Services.MapIndexDataService iService = new Services.MapIndexDataService(new DataRepo.Caching.MapCacheDataRepository(new DataRepo.MapDataRepository(),
          new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.AuthenticationService iAuth =
     new Services.AuthenticationService(new Data.Authentication.IAuthCacheRepository(new Data.Authentication.AuthRepository(),
         new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.LeafletService iLeafService =
    new Services.LeafletService(new LeafletHelpers.LeafletCacheScriptHelper(new LeafletHelpers.LeafletScriptHelper(),
  new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.ExportService iExport = new Services.ExportService(new Infrastructure.ExportHelpers.MA.MaCacheExportHelper(new Infrastructure.ExportHelpers.MA.MaExportHelpers(), new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.DfsExportService iDfsExport = new Services.DfsExportService(new Infrastructure.ExportHelpers.DFS.DfsCacheExportHelper(new Infrastructure.ExportHelpers.DFS.DfsExportHelpers(), new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.PortfolioExportService iPortExport = new Services.PortfolioExportService(new Infrastructure.ExportHelpers.Portfolio.PortfolioCacheExportHelper(new Infrastructure.ExportHelpers.Portfolio.PortfolioExportHelper(),
            new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.ChartingService iMaChart =
    new Services.ChartingService(new ChartingData.MA.MaChartingCacheData(new ChartingData.MA.MaChartingData(), new Infrastructure.Caching.MemoryCacheProvider()));


        private Services.MapIndexGeoserverService<DataRepo.IMapDataSet> iMaDataGeoService = new Services.MapIndexGeoserverService<DataRepo.IMapDataSet>(
    new GeoserverHelpers.MapIndex.MapIndexGeoserverCacheHelper<DataRepo.IMapDataSet>(new GeoserverHelpers.MapIndex.MapIndexGeoserverHelper<DataRepo.IMapDataSet>(),
      new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.MaViewHelperService iMaViewService = new Services.MaViewHelperService(
new GeoserverHelpers.MA.MaCacheClientHelper(new GeoserverHelpers.MA.MaClientHelpers(),
new Infrastructure.Caching.MemoryCacheProvider()));


        private object DfsMarkers(List<DataRepo.IMapDataSet> results, MapSearch map, SideFilterOptions sideFilters)
        {
            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            List<DfsData> dfsData = new List<DfsData>();
            List<DfsMap> dfsMap = new List<DfsMap>();

            foreach (DataRepo.IMapDataSet d in results)
            {
                var a = (DfsMap)Convert.ChangeType(d, typeof(DfsMap));
                dfsMap.Add(a);
            }

            dfsData = dfsMap.Select(item => new DfsData()
            {
                id = item.id,
                AgentId = item.AgentId,
                SellerId = item.SellerId,
                Listing_Date = item.Listing_Date,
                DBID = item.DBID,
                DealType = item.DealType,
                DealTypeId = item.DealTypeId,
                Headline = item.Headline,
                HexColor = item.HexColor,
                Icon = item.Icon,
                Lat = item.Lat,
                Link = item.Link,
                Long = item.Long,
                PlayId = item.PlayId,
                RegionId = item.RegionId,
                SellerLogo = item.SellerLogo,
                Sellers = item.Sellers,
                SourceDocs = item.SourceDocs,
                theId = item.theId,
                US_Play = item.US_Play,
                US_Region = item.US_Region
            }).ToList();

            Infrastructure.ExportHelpers.IExportData export = iDfsExport.GetChecklistTable(dfsData.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

            Models.ExportData.ExportDataSet exportSet = ((Models.ExportData.ExportDataSet)export);
            //Models.ExportData.ExportDataSet exportSet = new Models.ExportData.ExportDataSet();
           
            //Models.Charting.ChartOptions chartOptions = null;
            string cql = "";

            if (hasFilters && !string.IsNullOrEmpty(map.Keyword))
            {
                cql = GeoserverHelpers.GeoserverCqlFilter.CreateDFSCql(dfsData, map);
            }
            else if (hasFilters)
            {
                cql = GeoserverHelpers.GeoserverCqlFilter.CreateDFSCql(dfsData, map);
            }

            string companiesHtml = "";

            if ((map.ReloadCompanies || map.InitialLoad))
            {
                string cacheKey = ConfigurationFactory.Instance.Configuration().CacheKeyCompanyHtml + "_" + map.WhichDb;

                if (map.Plays.Count > 0)
                {
                    cacheKey += "_" + "PLAYS" + string.Join(",", map.Plays);
                }
                if (map.Regions.Count > 0)
                {
                    cacheKey += "_" + "REGIONS" + string.Join(",", map.Regions);

                }
                if (map.Companies.Count > 0)
                {
                    cacheKey += "_" + "COMPANIES" + string.Join(",", map.Companies);

                }
                if (map.Entities.Count > 0)
                {
                    cacheKey += "_" + "DEALTYPES" + string.Join(",", map.Entities);

                }
                if (!string.IsNullOrEmpty(map.TimeRange))
                {
                    cacheKey += "_" + "TIMERANGE" + map.TimeRange;

                }
                if (!string.IsNullOrEmpty(map.Keyword))
                {
                    cacheKey += "_" + "KEYWORD" + map.Keyword;

                }
                companiesHtml = PopupTemplating.PopupTemplating<DfsMap>.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);
                //companiesHtml = iMaDataGeoService.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);
            }
            string cacheKeyTable = "DFSLISTTABLE";

            if (map.Plays.Count > 0)
            {
                cacheKeyTable += "_" + "PLAYS" + string.Join(",", map.Plays);
            }
            if (map.Regions.Count > 0)
            {
                cacheKeyTable += "_" + "REGIONS" + string.Join(",", map.Regions);
            }
            if (map.Companies.Count > 0)
            {
                cacheKeyTable += "_" + "COMPANIES" + string.Join(",", map.Companies);
            }
            if (map.Entities.Count > 0)
            {
                cacheKeyTable += "_" + "DEALTYPES" + string.Join(",", map.Entities);
            }
            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                cacheKeyTable += "_" + "TIMERANGE" + map.TimeRange;
            }
            if (!string.IsNullOrEmpty(map.Keyword))
            {
                cacheKeyTable += "_" + "KEYWORD" + map.Keyword;
            }
            string tableDataSet = PopupTemplating.PopupTemplating<DfsData>.GenerateTableDataDfs(dfsData, cacheKeyTable, "~/Views/Templates/DfsTableList.cshtml", map);
            List<string> autoComplete = new List<string>();
            var resu = new
            {
                Result = iLeafService.GetMarkerData(new List<MaData>(),dfsData, new List<Models.EP.ViewModel.EpViewModel>(), map, "", false),
                SideFilters = sideFilters,
                //PortData = "",
                CheckSideListData = exportSet.ExportSet,
                TableData = dfsData,
                CqlFilter = cql,
                ExportTableSet = exportSet.ExportSet,
                ExportHeaders = exportSet.ExportHeaders,
                ChartSeries = "",
                //ChartX = chartOptions.ChartXCategories,
                //ChartDataSeries = chartOptions.ChartSeries,
                CompanyHtml = companiesHtml,
                AutoCompleteSource = autoComplete,
                TableSource = tableDataSet
            };

            return resu;
        }


        private object PortfolioMarkers(List<DataRepo.IMapDataSet> results, MapSearch map, SideFilterOptions sideFilters)
        {
            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            List<PortfolioData> dfsData = new List<PortfolioData>();
            List<PortfolioMap> dfsMap = new List<PortfolioMap>();

            foreach (DataRepo.IMapDataSet d in results)
            {
                var a = (PortfolioMap)Convert.ChangeType(d, typeof(PortfolioMap));
                dfsMap.Add(a);
            }

            dfsData = dfsMap.Select(item => new PortfolioData()
            {
                Address = item.Address,
                City = item.City,
                CompanyId = item.CompanyId,
                CompanyName = item.CompanyName,
                Counties = item.Counties,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                Logo = item.Logo,
                Phone = item.Phone,
                State = item.State,
                States = item.States,
                Zip = item.Zip,
                HexColor = item.HexColor,
                PlayId = item.PlayId,
                RegionId = item.RegionId,
                US_Play = item.US_Play,
                US_Region = item.US_Region
            }).ToList();

            Infrastructure.ExportHelpers.IExportData export = iPortExport.GetChecklistTable(dfsData.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

            Models.ExportData.ExportDataSet exportSet = ((Models.ExportData.ExportDataSet)export);
            //Models.ExportData.ExportDataSet exportSet = new Models.ExportData.ExportDataSet();

            //Models.Charting.ChartOptions chartOptions = null;
            string cql = "";

            if (hasFilters && !string.IsNullOrEmpty(map.Keyword))
            {
                cql = GeoserverHelpers.GeoserverCqlFilter.CreatePortfolioCql(dfsData, map);
            }
            else if (hasFilters)
            {
                cql = GeoserverHelpers.GeoserverCqlFilter.CreatePortfolioCql(dfsData, map);
            }

            string companiesHtml = "";

            if ((map.ReloadCompanies || map.InitialLoad))
            {
                string cacheKey = ConfigurationFactory.Instance.Configuration().CacheKeyCompanyHtml + "_" + map.WhichDb;

                if (map.Plays.Count > 0)
                {
                    cacheKey += "_" + "PLAYS" + string.Join(",", map.Plays);
                }
                if (map.Regions.Count > 0)
                {
                    cacheKey += "_" + "REGIONS" + string.Join(",", map.Regions);

                }
                if (map.Companies.Count > 0)
                {
                    cacheKey += "_" + "COMPANIES" + string.Join(",", map.Companies);

                }
                if (map.Entities.Count > 0)
                {
                    cacheKey += "_" + "DEALTYPES" + string.Join(",", map.Entities);

                }
                if (!string.IsNullOrEmpty(map.TimeRange))
                {
                    cacheKey += "_" + "TIMERANGE" + map.TimeRange;

                }
                if (!string.IsNullOrEmpty(map.Keyword))
                {
                    cacheKey += "_" + "KEYWORD" + map.Keyword;

                }
                companiesHtml = PopupTemplating.PopupTemplating<PortfolioMap>.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);
                //companiesHtml = iMaDataGeoService.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);
            }
            string cacheKeyTable = "PORTFOLIOLISTTABLE";

            if (map.Plays.Count > 0)
            {
                cacheKeyTable += "_" + "PLAYS" + string.Join(",", map.Plays);
            }
            if (map.Regions.Count > 0)
            {
                cacheKeyTable += "_" + "REGIONS" + string.Join(",", map.Regions);
            }
            if (map.Companies.Count > 0)
            {
                cacheKeyTable += "_" + "COMPANIES" + string.Join(",", map.Companies);
            }
            if (map.Entities.Count > 0)
            {
                cacheKeyTable += "_" + "DEALTYPES" + string.Join(",", map.Entities);
            }
            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                cacheKeyTable += "_" + "TIMERANGE" + map.TimeRange;
            }
            if (!string.IsNullOrEmpty(map.Keyword))
            {
                cacheKeyTable += "_" + "KEYWORD" + map.Keyword;
            }
            string tableDataSet = PopupTemplating.PopupTemplating<PortfolioData>.GenerateTableDataPortfolio(dfsData, cacheKeyTable, "~/Views/Templates/PortfolioTableList.cshtml", map);
            List<string> autoComplete = new List<string>();
            var resu = new
            {
                Result = iLeafService.GetMarkerData(new List<MaData>(), new List<DfsData>(), new List<Models.EP.ViewModel.EpViewModel>(), map, "", false),
                SideFilters = sideFilters,
                //PortData = "",
                CheckSideListData = exportSet.ExportSet,
                TableData = dfsData,
                CqlFilter = cql,
                ExportTableSet = exportSet.ExportSet,
                ExportHeaders = exportSet.ExportHeaders,
                ChartSeries = "",
                //ChartX = chartOptions.ChartXCategories,
                //ChartDataSeries = chartOptions.ChartSeries,
                CompanyHtml = companiesHtml,
                AutoCompleteSource = autoComplete,
                TableSource = tableDataSet
            };

            return resu;
        }

        private string TableSourceSet(List<DataRepo.IMapDataSet> results, MapSearch map)
        {
            string result = "";

            string cacheKeyTable = map.WhichDb + "_" + "LISTTABLE";

            if (map.Plays.Count > 0)
            {
                cacheKeyTable += "_" + "PLAYS" + string.Join(",", map.Plays);
            }
            if (map.Regions.Count > 0)
            {
                cacheKeyTable += "_" + "REGIONS" + string.Join(",", map.Regions);
            }
            if (map.Companies.Count > 0)
            {
                cacheKeyTable += "_" + "COMPANIES" + string.Join(",", map.Companies);
            }
            if (map.Entities.Count > 0)
            {
                cacheKeyTable += "_" + "DEALTYPES" + string.Join(",", map.Entities);
            }
            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                cacheKeyTable += "_" + "TIMERANGE" + map.TimeRange;
            }
            if (!string.IsNullOrEmpty(map.Keyword))
            {
                cacheKeyTable += "_" + "KEYWORD" + map.Keyword;
            }

            if (map.WhichDb == "MATAB")
            {
                List<MaData> maResult = new List<MaData>();
                List<MaMap> maResultss = new List<MaMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (MaMap)Convert.ChangeType(d, typeof(MaMap));
                    maResultss.Add(a);
                }

                #region
                maResult = maResultss.Select(item => new MaData()
                {
                    id = item.id,
                    BuyerIds = item.BuyerIds,
                    BuyerLogo = item.BuyerLogo,
                    BuyerIdsString = item.BuyerIdsString,
                    Buyers = item.Buyers,
                    Counties = item.Counties,
                    Date_Announced = item.Date_Announced,
                    DBID = item.DBID,
                    DealType = item.DealType,
                    DealTypeId = item.DealTypeId,
                    Headline = item.Headline,
                    HexColor = item.HexColor,
                    Hydrocarbon = item.Hydrocarbon,
                    Icon = item.Icon,
                    Lat = item.Lat,
                    Link = item.Link,
                    Long = item.Long,
                    OpNonOp = item.OpNonOp,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    Resource_Type = item.Resource_Type,
                    SellerIds = item.SellerIds,
                    SellerIdsString = item.SellerIdsString,
                    SellerLogo = item.SellerLogo,
                    Sellers = item.Sellers,
                    SourceDocs = item.SourceDocs,
                    States = item.States,
                    theId = item.theId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region,
                    Value___MM_ = item.Value___MM_,
                    __Acre = item.__Acre,
                    __Daily_BOE = item.__Daily_BOE
                }).ToList();
                #endregion
                result = PopupTemplating.PopupTemplating<MaData>.GenerateTableDataMa(maResult, cacheKeyTable, "~/Views/Templates/MaTableList.cshtml", map);

            }
            else if (map.WhichDb == "DFSTAB")
            {
                List<DfsData> dfsData = new List<DfsData>();
                List<DfsMap> dfsMap = new List<DfsMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (DfsMap)Convert.ChangeType(d, typeof(DfsMap));
                    dfsMap.Add(a);
                }
                #region
                dfsData = dfsMap.Select(item => new DfsData()
                {
                    id = item.id,
                    AgentId = item.AgentId,
                    SellerId = item.SellerId,
                    Listing_Date = item.Listing_Date,
                    DBID = item.DBID,
                    DealType = item.DealType,
                    DealTypeId = item.DealTypeId,
                    Headline = item.Headline,
                    HexColor = item.HexColor,
                    Icon = item.Icon,
                    Lat = item.Lat,
                    Link = item.Link,
                    Long = item.Long,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    SellerLogo = item.SellerLogo,
                    Sellers = item.Sellers,
                    SourceDocs = item.SourceDocs,
                    theId = item.theId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region
                }).ToList();
                #endregion
                result = PopupTemplating.PopupTemplating<DfsData>.GenerateTableDataDfs(dfsData, cacheKeyTable, "~/Views/Templates/DfsTableList.cshtml", map);

            }
            else if (map.WhichDb == "PORTFOLIOTAB")
            {
                List<PortfolioData> portData = new List<PortfolioData>();
                List<PortfolioMap> portMap = new List<PortfolioMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (PortfolioMap)Convert.ChangeType(d, typeof(PortfolioMap));
                    portMap.Add(a);
                }
                #region
                portData = portMap.Select(item => new PortfolioData()
                {
                    Address = item.Address,
                    City = item.City,
                    CompanyId = item.CompanyId,
                    CompanyName = item.CompanyName,
                    Counties = item.Counties,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    Logo = item.Logo,
                    Phone = item.Phone,
                    State = item.State,
                    States = item.States,
                    Zip = item.Zip,
                    HexColor = item.HexColor,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region
                }).ToList();
                #endregion
                result = PopupTemplating.PopupTemplating<PortfolioData>.GenerateTableDataPortfolio(portData, cacheKeyTable, "~/Views/Templates/PortfolioTableList.cshtml", map);
            }

            return result;
        }

        private Models.ExportData.ExportDataSet FetchExportSet(List<DataRepo.IMapDataSet> results, MapSearch map)
        {
            Models.ExportData.ExportDataSet exportSet = new Models.ExportData.ExportDataSet();
            if (map.WhichDb == "MATAB")
            {
                List<MaData> maResult = new List<MaData>();
                List<MaMap> maResultss = new List<MaMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (MaMap)Convert.ChangeType(d, typeof(MaMap));
                    maResultss.Add(a);
                }

                #region
                maResult = maResultss.Select(item => new MaData()
                {
                    id = item.id,
                    BuyerIds = item.BuyerIds,
                    BuyerLogo = item.BuyerLogo,
                    BuyerIdsString = item.BuyerIdsString,
                    Buyers = item.Buyers,
                    Counties = item.Counties,
                    Date_Announced = item.Date_Announced,
                    DBID = item.DBID,
                    DealType = item.DealType,
                    DealTypeId = item.DealTypeId,
                    Headline = item.Headline,
                    HexColor = item.HexColor,
                    Hydrocarbon = item.Hydrocarbon,
                    Icon = item.Icon,
                    Lat = item.Lat,
                    Link = item.Link,
                    Long = item.Long,
                    OpNonOp = item.OpNonOp,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    Resource_Type = item.Resource_Type,
                    SellerIds = item.SellerIds,
                    SellerIdsString = item.SellerIdsString,
                    SellerLogo = item.SellerLogo,
                    Sellers = item.Sellers,
                    SourceDocs = item.SourceDocs,
                    States = item.States,
                    theId = item.theId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region,
                    Value___MM_ = item.Value___MM_,
                    __Acre = item.__Acre,
                    __Daily_BOE = item.__Daily_BOE
                }).ToList();
                #endregion
                Infrastructure.ExportHelpers.IExportData export = iExport.GetChecklistTable(maResult.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

                exportSet = ((Models.ExportData.ExportDataSet)export);

            }
            else if (map.WhichDb == "DFSTAB")
            {
                List<DfsData> dfsData = new List<DfsData>();
                List<DfsMap> dfsMap = new List<DfsMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (DfsMap)Convert.ChangeType(d, typeof(DfsMap));
                    dfsMap.Add(a);
                }
                #region
                dfsData = dfsMap.Select(item => new DfsData()
                {
                    id = item.id,
                    AgentId = item.AgentId,
                    SellerId = item.SellerId,
                    Listing_Date = item.Listing_Date,
                    DBID = item.DBID,
                    DealType = item.DealType,
                    DealTypeId = item.DealTypeId,
                    Headline = item.Headline,
                    HexColor = item.HexColor,
                    Icon = item.Icon,
                    Lat = item.Lat,
                    Link = item.Link,
                    Long = item.Long,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    SellerLogo = item.SellerLogo,
                    Sellers = item.Sellers,
                    SourceDocs = item.SourceDocs,
                    theId = item.theId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region
                }).ToList();
                #endregion
                Infrastructure.ExportHelpers.IExportData export = iDfsExport.GetChecklistTable(dfsData.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

                exportSet = ((Models.ExportData.ExportDataSet)export);

            }
            else if (map.WhichDb == "PORTFOLIOTAB")
            {
                List<PortfolioData> portData = new List<PortfolioData>();
                List<PortfolioMap> portMap = new List<PortfolioMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (PortfolioMap)Convert.ChangeType(d, typeof(PortfolioMap));
                    portMap.Add(a);
                }
                #region
                portData = portMap.Select(item => new PortfolioData()
                {
                    Address = item.Address,
                    City = item.City,
                    CompanyId = item.CompanyId,
                    CompanyName = item.CompanyName,
                    Counties = item.Counties,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    Logo = item.Logo,
                    Phone = item.Phone,
                    State = item.State,
                    States = item.States,
                    Zip = item.Zip,
                    HexColor = item.HexColor,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region
                }).ToList();
                #endregion
                Infrastructure.ExportHelpers.IExportData export = iPortExport.GetChecklistTable(portData.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

                exportSet = ((Models.ExportData.ExportDataSet)export);
            }


            return exportSet;
        }

        private Models.Charting.ChartOptions FetchChartOptions(List<DataRepo.IMapDataSet> results, MapSearch map)
        {

            Models.Charting.ChartOptions chartOptions = new Models.Charting.ChartOptions();

            if (map.WhichDb == "MATAB")
            {
                List<MaData> maResult = new List<MaData>();
                List<MaMap> maResultss = new List<MaMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (MaMap)Convert.ChangeType(d, typeof(MaMap));
                    maResultss.Add(a);
                }

                #region
                maResult = maResultss.Select(item => new MaData()
                {
                    id = item.id,
                    BuyerIds = item.BuyerIds,
                    BuyerLogo = item.BuyerLogo,
                    BuyerIdsString = item.BuyerIdsString,
                    Buyers = item.Buyers,
                    Counties = item.Counties,
                    Date_Announced = item.Date_Announced,
                    DBID = item.DBID,
                    DealType = item.DealType,
                    DealTypeId = item.DealTypeId,
                    Headline = item.Headline,
                    HexColor = item.HexColor,
                    Hydrocarbon = item.Hydrocarbon,
                    Icon = item.Icon,
                    Lat = item.Lat,
                    Link = item.Link,
                    Long = item.Long,
                    OpNonOp = item.OpNonOp,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    Resource_Type = item.Resource_Type,
                    SellerIds = item.SellerIds,
                    SellerIdsString = item.SellerIdsString,
                    SellerLogo = item.SellerLogo,
                    Sellers = item.Sellers,
                    SourceDocs = item.SourceDocs,
                    States = item.States,
                    theId = item.theId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region,
                    Value___MM_ = item.Value___MM_,
                    __Acre = item.__Acre,
                    __Daily_BOE = item.__Daily_BOE
                }).ToList();
                #endregion
                ChartingData.IChartOutput charts = iMaChart.GetChartOutput(maResult, map);
                chartOptions = ((Models.Charting.ChartOptions)charts);
            }
            

            return chartOptions;
        }

        private string FetchCompaniesHtml(MapSearch map, List<Company> companies)
        {
            string result = "";
            string cacheKey = ConfigurationFactory.Instance.Configuration().CacheKeyCompanyHtml + "_" + map.WhichDb;

            if (map.Plays.Count > 0)
            {
                cacheKey += "_" + "PLAYS" + string.Join(",", map.Plays);
            }
            if (map.Regions.Count > 0)
            {
                cacheKey += "_" + "REGIONS" + string.Join(",", map.Regions);

            }
            if (map.Companies.Count > 0)
            {
                cacheKey += "_" + "COMPANIES" + string.Join(",", map.Companies);

            }
            if (map.Entities.Count > 0)
            {
                cacheKey += "_" + "DEALTYPES" + string.Join(",", map.Entities);

            }
            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                cacheKey += "_" + "TIMERANGE" + map.TimeRange;

            }
            if (!string.IsNullOrEmpty(map.Keyword))
            {
                cacheKey += "_" + "KEYWORD" + map.Keyword;

            }

            if (map.WhichDb == "MATAB")
            {
                result = PopupTemplating.PopupTemplating<MaMap>.GenerateCompanyHtml(companies,
    cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);

            }
            else if (map.WhichDb == "DFSTAB")
            {
                result = PopupTemplating.PopupTemplating<DfsMap>.GenerateCompanyHtml(companies,
    cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);

            }
            else if (map.WhichDb == "PORTFOLIOTAB")
            {
                result = PopupTemplating.PopupTemplating<PortfolioMap>.GenerateCompanyHtml(companies,
    cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);

            }

            return result;
        }

        private string GenerateCql(bool hasFilters, MapSearch map, List<DataRepo.IMapDataSet> results)
        {
            string cql = "";

            if (hasFilters)
            {
                if (map.WhichDb == "MATAB")
                {
                    List<MaData> maResult = new List<MaData>();
                    List<MaMap> maResultss = new List<MaMap>();

                    foreach (DataRepo.IMapDataSet d in results)
                    {
                        var a = (MaMap)Convert.ChangeType(d, typeof(MaMap));
                        maResultss.Add(a);
                    }

                    #region
                    maResult = maResultss.Select(item => new MaData()
                    {
                        id = item.id,
                        BuyerIds = item.BuyerIds,
                        BuyerLogo = item.BuyerLogo,
                        BuyerIdsString = item.BuyerIdsString,
                        Buyers = item.Buyers,
                        Counties = item.Counties,
                        Date_Announced = item.Date_Announced,
                        DBID = item.DBID,
                        DealType = item.DealType,
                        DealTypeId = item.DealTypeId,
                        Headline = item.Headline,
                        HexColor = item.HexColor,
                        Hydrocarbon = item.Hydrocarbon,
                        Icon = item.Icon,
                        Lat = item.Lat,
                        Link = item.Link,
                        Long = item.Long,
                        OpNonOp = item.OpNonOp,
                        PlayId = item.PlayId,
                        RegionId = item.RegionId,
                        Resource_Type = item.Resource_Type,
                        SellerIds = item.SellerIds,
                        SellerIdsString = item.SellerIdsString,
                        SellerLogo = item.SellerLogo,
                        Sellers = item.Sellers,
                        SourceDocs = item.SourceDocs,
                        States = item.States,
                        theId = item.theId,
                        US_Play = item.US_Play,
                        US_Region = item.US_Region,
                        Value___MM_ = item.Value___MM_,
                        __Acre = item.__Acre,
                        __Daily_BOE = item.__Daily_BOE
                    }).ToList();
                    #endregion
                    cql = GeoserverHelpers.GeoserverCqlFilter.CreateMACql(maResult, map);

                }
                else if (map.WhichDb == "DFSTAB")
                {
                    List<DfsData> dfsData = new List<DfsData>();
                    List<DfsMap> dfsMap = new List<DfsMap>();

                    foreach (DataRepo.IMapDataSet d in results)
                    {
                        var a = (DfsMap)Convert.ChangeType(d, typeof(DfsMap));
                        dfsMap.Add(a);
                    }
                    #region
                    dfsData = dfsMap.Select(item => new DfsData()
                    {
                        id = item.id,
                        AgentId = item.AgentId,
                        SellerId = item.SellerId,
                        Listing_Date = item.Listing_Date,
                        DBID = item.DBID,
                        DealType = item.DealType,
                        DealTypeId = item.DealTypeId,
                        Headline = item.Headline,
                        HexColor = item.HexColor,
                        Icon = item.Icon,
                        Lat = item.Lat,
                        Link = item.Link,
                        Long = item.Long,
                        PlayId = item.PlayId,
                        RegionId = item.RegionId,
                        SellerLogo = item.SellerLogo,
                        Sellers = item.Sellers,
                        SourceDocs = item.SourceDocs,
                        theId = item.theId,
                        US_Play = item.US_Play,
                        US_Region = item.US_Region
                    }).ToList();
                    #endregion
                    cql = GeoserverHelpers.GeoserverCqlFilter.CreateDFSCql(dfsData, map);
                }
                else if (map.WhichDb == "PORTFOLIOTAB")
                {
                    List<PortfolioData> portData = new List<PortfolioData>();
                    List<PortfolioMap> portMap = new List<PortfolioMap>();

                    foreach (DataRepo.IMapDataSet d in results)
                    {
                        var a = (PortfolioMap)Convert.ChangeType(d, typeof(PortfolioMap));
                        portMap.Add(a);
                    }
                    #region
                    portData = portMap.Select(item => new PortfolioData()
                    {
                        Address = item.Address,
                        City = item.City,
                        CompanyId = item.CompanyId,
                        CompanyName = item.CompanyName,
                        Counties = item.Counties,
                        Latitude = item.Latitude,
                        Longitude = item.Longitude,
                        Logo = item.Logo,
                        Phone = item.Phone,
                        State = item.State,
                        States = item.States,
                        Zip = item.Zip,
                        HexColor = item.HexColor,
                        PlayId = item.PlayId,
                        RegionId = item.RegionId,
                        US_Play = item.US_Play,
                        US_Region = item.US_Region
                    }).ToList();
                    #endregion
                    cql = GeoserverHelpers.GeoserverCqlFilter.CreatePortfolioCql(portData, map);

                }
            }
            
            return cql;
        }

        private List<Models.Leaflet.LeafletMarker> LeafletMarkers(List<DataRepo.IMapDataSet> results, MapSearch map)
        {
            List<Models.Leaflet.LeafletMarker> markers = new List<Models.Leaflet.LeafletMarker>();

            if (map.WhichDb == "MATAB")
            {
                List<MaData> maResult = new List<MaData>();
                List<MaMap> maResultss = new List<MaMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (MaMap)Convert.ChangeType(d, typeof(MaMap));
                    maResultss.Add(a);
                }

                #region
                maResult = maResultss.Select(item => new MaData()
                {
                    id = item.id,
                    BuyerIds = item.BuyerIds,
                    BuyerLogo = item.BuyerLogo,
                    BuyerIdsString = item.BuyerIdsString,
                    Buyers = item.Buyers,
                    Counties = item.Counties,
                    Date_Announced = item.Date_Announced,
                    DBID = item.DBID,
                    DealType = item.DealType,
                    DealTypeId = item.DealTypeId,
                    Headline = item.Headline,
                    HexColor = item.HexColor,
                    Hydrocarbon = item.Hydrocarbon,
                    Icon = item.Icon,
                    Lat = item.Lat,
                    Link = item.Link,
                    Long = item.Long,
                    OpNonOp = item.OpNonOp,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    Resource_Type = item.Resource_Type,
                    SellerIds = item.SellerIds,
                    SellerIdsString = item.SellerIdsString,
                    SellerLogo = item.SellerLogo,
                    Sellers = item.Sellers,
                    SourceDocs = item.SourceDocs,
                    States = item.States,
                    theId = item.theId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region,
                    Value___MM_ = item.Value___MM_,
                    __Acre = item.__Acre,
                    __Daily_BOE = item.__Daily_BOE
                }).ToList();
                #endregion
                markers = iLeafService.GetMarkerData(maResult, new List<DfsData>(),
                    new List<Models.EP.ViewModel.EpViewModel>(), map, "", false);
            }
            else if (map.WhichDb == "DFSTAB")
            {
                List<DfsData> dfsData = new List<DfsData>();
                List<DfsMap> dfsMap = new List<DfsMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (DfsMap)Convert.ChangeType(d, typeof(DfsMap));
                    dfsMap.Add(a);
                }
                #region
                dfsData = dfsMap.Select(item => new DfsData()
                {
                    id = item.id,
                    AgentId = item.AgentId,
                    SellerId = item.SellerId,
                    Listing_Date = item.Listing_Date,
                    DBID = item.DBID,
                    DealType = item.DealType,
                    DealTypeId = item.DealTypeId,
                    Headline = item.Headline,
                    HexColor = item.HexColor,
                    Icon = item.Icon,
                    Lat = item.Lat,
                    Link = item.Link,
                    Long = item.Long,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    SellerLogo = item.SellerLogo,
                    Sellers = item.Sellers,
                    SourceDocs = item.SourceDocs,
                    theId = item.theId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region
                }).ToList();
                #endregion
                markers = iLeafService.GetMarkerData(new List<MaData>(), dfsData,
                   new List<Models.EP.ViewModel.EpViewModel>(), map, "", false);
            }
            else if (map.WhichDb == "PORTFOLIOTAB")
            {
                List<PortfolioData> portData = new List<PortfolioData>();
                List<PortfolioMap> portMap = new List<PortfolioMap>();

                foreach (DataRepo.IMapDataSet d in results)
                {
                    var a = (PortfolioMap)Convert.ChangeType(d, typeof(PortfolioMap));
                    portMap.Add(a);
                }
                #region
                portData = portMap.Select(item => new PortfolioData()
                {
                    Address = item.Address,
                    City = item.City,
                    CompanyId = item.CompanyId,
                    CompanyName = item.CompanyName,
                    Counties = item.Counties,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    Logo = item.Logo,
                    Phone = item.Phone,
                    State = item.State,
                    States = item.States,
                    Zip = item.Zip,
                    HexColor = item.HexColor,
                    PlayId = item.PlayId,
                    RegionId = item.RegionId,
                    US_Play = item.US_Play,
                    US_Region = item.US_Region
                }).ToList();
                #endregion
                markers = iLeafService.GetMarkerData(new List<MaData>(), new List<DfsData>(),
              new List<Models.EP.ViewModel.EpViewModel>(), map, "", false);
            }


            return markers;
        }

        //private object MaMarkers(List<DataRepo.IMapDataSet> results, MapSearch map, SideFilterOptions sideFilters)
        //{
        //    var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

        //    List<MaData> maResult = new List<MaData>();
        //    List<MaMap> maResultss = new List<MaMap>();

        //    foreach (DataRepo.IMapDataSet d in results)
        //    {
        //        var a = (MaMap)Convert.ChangeType(d, typeof(MaMap));
        //        maResultss.Add(a);
        //    }

        //    #region
        //    maResult = maResultss.Select(item => new MaData()
        //    {
        //        id = item.id,
        //        BuyerIds = item.BuyerIds,
        //        BuyerLogo = item.BuyerLogo,
        //        BuyerIdsString = item.BuyerIdsString,
        //        Buyers = item.Buyers,
        //        Counties = item.Counties,
        //        Date_Announced = item.Date_Announced,
        //        DBID = item.DBID,
        //        DealType = item.DealType,
        //        DealTypeId = item.DealTypeId,
        //        Headline = item.Headline,
        //        HexColor = item.HexColor,
        //        Hydrocarbon = item.Hydrocarbon,
        //        Icon = item.Icon,
        //        Lat = item.Lat,
        //        Link = item.Link,
        //        Long = item.Long,
        //        OpNonOp = item.OpNonOp,
        //        PlayId = item.PlayId,
        //        RegionId = item.RegionId,
        //        Resource_Type = item.Resource_Type,
        //        SellerIds = item.SellerIds,
        //        SellerIdsString = item.SellerIdsString,
        //        SellerLogo = item.SellerLogo,
        //        Sellers = item.Sellers,
        //        SourceDocs = item.SourceDocs,
        //        States = item.States,
        //        theId = item.theId,
        //        US_Play = item.US_Play,
        //        US_Region = item.US_Region,
        //        Value___MM_ = item.Value___MM_,
        //        __Acre = item.__Acre,
        //        __Daily_BOE = item.__Daily_BOE
        //    }).ToList();
        //    #endregion


        //    //Infrastructure.ExportHelpers.IExportData export = iExport.GetChecklistTable(maResult.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

        //    //Models.ExportData.ExportDataSet exportSet = ((Models.ExportData.ExportDataSet)export);
        //    //Models.ExportData.ExportDataSet exportSet = new Models.ExportData.ExportDataSet();
        //    //ChartingData.IChartOutput charts = iMaChart.GetChartOutput(maResult, map);
        //    //ChartingData.IChartOutput charts = null;
        //    //Models.Charting.ChartOptions chartOptions = ((Models.Charting.ChartOptions)charts);
        //    //Models.Charting.ChartOptions chartOptions = null;
        //    string cql = "";

        //    if (hasFilters && !string.IsNullOrEmpty(map.Keyword))
        //    {
        //        cql = GeoserverHelpers.GeoserverCqlFilter.CreateMACql(maResult, map);
        //    }
        //    else if (hasFilters)
        //    {
        //        cql = GeoserverHelpers.GeoserverCqlFilter.CreateMACql(maResult, map);
        //    }

        //    string companiesHtml = "";

        //    //if ((map.ReloadCompanies || map.InitialLoad))
        //    //{
        //    //    string cacheKey = ConfigurationFactory.Instance.Configuration().CacheKeyCompanyHtml + "_" + map.WhichDb;

        //    //    if (map.Plays.Count > 0)
        //    //    {
        //    //        cacheKey += "_" + "PLAYS" + string.Join(",", map.Plays);
        //    //    }
        //    //    if (map.Regions.Count > 0)
        //    //    {
        //    //        cacheKey += "_" + "REGIONS" + string.Join(",", map.Regions);

        //    //    }
        //    //    if (map.Companies.Count > 0)
        //    //    {
        //    //        cacheKey += "_" + "COMPANIES" + string.Join(",", map.Companies);

        //    //    }
        //    //    if (map.Entities.Count > 0)
        //    //    {
        //    //        cacheKey += "_" + "DEALTYPES" + string.Join(",", map.Entities);

        //    //    }
        //    //    if (!string.IsNullOrEmpty(map.TimeRange))
        //    //    {
        //    //        cacheKey += "_" + "TIMERANGE" + map.TimeRange;

        //    //    }
        //    //    if (!string.IsNullOrEmpty(map.Keyword))
        //    //    {
        //    //        cacheKey += "_" + "KEYWORD" + map.Keyword;

        //    //    }
        //    //    companiesHtml = PopupTemplating.PopupTemplating<MaMap>.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);
        //    //    //companiesHtml = iMaDataGeoService.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);
        //    //}
        //        string cacheKeyTable = "MALISTTABLE";

        //        if (map.Plays.Count > 0)
        //        {
        //            cacheKeyTable += "_" + "PLAYS" + string.Join(",", map.Plays);
        //        }
        //        if (map.Regions.Count > 0)
        //        {
        //            cacheKeyTable += "_" + "REGIONS" + string.Join(",", map.Regions);
        //        }
        //        if (map.Companies.Count > 0)
        //        {
        //            cacheKeyTable += "_" + "COMPANIES" + string.Join(",", map.Companies);
        //        }
        //        if (map.Entities.Count > 0)
        //        {
        //            cacheKeyTable += "_" + "DEALTYPES" + string.Join(",", map.Entities);
        //        }
        //        if (!string.IsNullOrEmpty(map.TimeRange))
        //        {
        //            cacheKeyTable += "_" + "TIMERANGE" + map.TimeRange;
        //        }
        //        if (!string.IsNullOrEmpty(map.Keyword))
        //        {
        //            cacheKeyTable += "_" + "KEYWORD" + map.Keyword;
        //        }
        //        string tableDataSet = PopupTemplating.PopupTemplating<MaData>.GenerateTableDataMa(maResult, cacheKeyTable, "~/Views/Templates/MaTableList.cshtml", map);
        //        List<string> autoComplete = new List<string>();
        //        var resu = new
        //        {
        //            Result = iLeafService.GetMarkerData(maResult, new List<DfsData>(), new List<Models.EP.ViewModel.EpViewModel>(), map, "", false),
        //            SideFilters = sideFilters,
        //            //PortData = "",
        //            CheckSideListData = exportSet.ExportSet,
        //            TableData = maResult,
        //            CqlFilter = cql,
        //            ExportTableSet = exportSet.ExportSet,
        //            ExportHeaders = exportSet.ExportHeaders,
        //            ChartSeries = "",
        //            ChartX = chartOptions.ChartXCategories,
        //            ChartDataSeries = chartOptions.ChartSeries,
        //            CompanyHtml = companiesHtml,
        //            AutoCompleteSource = autoComplete,
        //            TableSource = tableDataSet
        //        };

        //    return resu;
        //}


        private SideFilterOptions FetchSideFilters(List<DataRepo.IMapDataSet> data, MapSearch map, List<DataRepo.IMapDataSet> filterData)
        {
            SideFilterOptions sideFilters = new SideFilterOptions();


            bool hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            if (hasFilters)
            {
                List<DataRepo.IMapDataSet> sideFilterData = new List<DataRepo.IMapDataSet>();

                //filterData = iService.GetData(map, data);

                if (!string.IsNullOrEmpty(map.Keyword) || map.Companies.Count > 0)
                {
                    MapSearch mapNoCompanies = (MapSearch)map.ShallowCopy();
                    mapNoCompanies.Companies = new List<string>();
                    sideFilterData = iService.GetData(mapNoCompanies,data);
                }
                else
                {
                    sideFilterData = filterData;
                }

                sideFilters = iService.GetSideFilters(sideFilterData, map);
            }
            else
            {
                sideFilters = iService.GetSideFilters(data, map);
                //filterData = initialDataSet;
            }

            return sideFilters;
        }

        [ActionName("GenerateMarkers")]
        [HttpPost]
        //[HttpGet]
        public async Task<IHttpActionResult> GenerateMarkers(MapSearch map)
        //public IHttpActionResult GenerateMarkers()
        {
            object result = null;

            Models.Authentication.ViewModels.AuthViewModel auth = Infrastructure.Session.AuthSession.GetAuthModel(map);

            map.TheSessionId = map.UserName + "_" + map.QbTicket + "_" + map.QbToken;
            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            if (iAuth.IsUserValid(auth))
            {

                // INITIAL DATA SET
                MapSearch mapInitial = new MapSearch();
                mapInitial.InitialLoad = true;
                mapInitial.WhichDb = map.WhichDb;
                List<DataRepo.IMapDataSet> initialDataSet = iService.GetData(mapInitial, new List<DataRepo.IMapDataSet>());

                //var t1 = Task.Run(() => iService.GetData(map));
                //var t2 = Task.Run(() => FetchSideFilters(initialDataSet, map));
                //await Task.WhenAll(t1, t2);

                //var data = new 
                //{
                //    d1 = t1.Status == TaskStatus.RanToCompletion ? t1.Result : null,
                //    d2 = t2.Status == TaskStatus.RanToCompletion ? t2.Result : null,
                //};


                #region
                //bool hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

                //List<DataRepo.IMapDataSet> initialDataSet = iService.GetData(mapInitial);
                //List<DataRepo.IMapDataSet> filterData = new List<DataRepo.IMapDataSet>();
                //List<DataRepo.IMapDataSet> sideFilterData = new List<DataRepo.IMapDataSet>();

                //if (hasFilters)
                //{
                //    filterData = iService.GetData(map);

                //    if (!string.IsNullOrEmpty(map.Keyword) || map.Companies.Count > 0)
                //    {
                //        MapSearch mapNoCompanies = (MapSearch)map.ShallowCopy();
                //        mapNoCompanies.Companies = new List<string>();
                //        sideFilterData = iService.GetData(mapNoCompanies);
                //    }
                //    else
                //    {
                //        sideFilterData = filterData;
                //    }

                //    sideFilters = iService.GetSideFilters(sideFilterData, map);

                //}
                //else
                //{
                //    sideFilters = iService.GetSideFilters(initialDataSet, map);
                //    //filterData = initialDataSet;
                //}
                #endregion

               
                    var filteredDataTask = Task.Run(() => iService.GetData(map, initialDataSet));
               
                    
                    await Task.WhenAll(filteredDataTask);

                    var filteredDataTaskData = new
                    {
                        filteredDataSet = filteredDataTask.Status == TaskStatus.RanToCompletion ? filteredDataTask.Result : null
                    };
                    var sideFiltersTask = Task.Run(() => FetchSideFilters(initialDataSet, map, filteredDataTaskData.filteredDataSet));

                    await Task.WhenAll(sideFiltersTask);

                    var dataSideFilters = new
                    {
                        sideFilterDataSet = sideFiltersTask.Status == TaskStatus.RanToCompletion ? sideFiltersTask.Result : null
                    };
                    var leafletTask = Task.Run(() => LeafletMarkers(filteredDataTaskData.filteredDataSet, map));

                    var exportTask = Task.Run(() => FetchExportSet(filteredDataTaskData.filteredDataSet, map));
                    var chartTask = Task.Run(() => FetchChartOptions(filteredDataTaskData.filteredDataSet, map));
                    var companyTask = Task.Run(() => FetchCompaniesHtml(map, dataSideFilters.sideFilterDataSet.Companies));
                    var tableSourceTask = Task.Run(() => TableSourceSet(filteredDataTaskData.filteredDataSet, map));
                    var cqlTask = Task.Run(() => GenerateCql(hasFilters, map, filteredDataTaskData.filteredDataSet));

                    await Task.WhenAll(leafletTask, exportTask, chartTask, companyTask, tableSourceTask, cqlTask);

                    var data2 = new
                    {
                        d3 = leafletTask.Status == TaskStatus.RanToCompletion ? leafletTask.Result : null,
                        d4 = exportTask.Status == TaskStatus.RanToCompletion ? exportTask.Result : null,
                        d5 = chartTask.Status == TaskStatus.RanToCompletion ? chartTask.Result : null,
                        d6 = companyTask.Status == TaskStatus.RanToCompletion ? companyTask.Result : null,
                        d7 = tableSourceTask.Status == TaskStatus.RanToCompletion ? tableSourceTask.Result : null,
                        d8 = cqlTask.Status == TaskStatus.RanToCompletion ? cqlTask.Result : null

                    };
                    result = new
                    {
                        Result = data2.d3,
                        SideFilters = dataSideFilters.sideFilterDataSet,
                        //PortData = "",
                        CheckSideListData = data2.d4.ExportSet,
                        TableData = filteredDataTaskData.filteredDataSet,
                        CqlFilter = data2.d8,
                        ExportTableSet = data2.d4.ExportSet,
                        ExportHeaders = data2.d4.ExportHeaders,
                        ChartSeries = "",
                        ChartX = data2.d5.ChartXCategories,
                        ChartDataSeries = data2.d5.ChartSeries,
                        CompanyHtml = data2.d6,
                        AutoCompleteSource = "",
                        TableSource = data2.d7
                    };

            }

            return Ok(result);
        }
    }
}
