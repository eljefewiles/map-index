using Ionic.Zip;
using MapIndex.Data;
using MapIndex.Infrastructure.Configuration;
using MapIndex.Models;
using MapIndex.Models.DFS;
using MapIndex.Models.EP.ViewModel;
using MapIndex.Models.MA;
using MapIndex.Models.Portfolio;
using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;

namespace MapIndex.Controllers
{

    public class MapIndexController : ApiController
    {
        private Services.MaMapIndexService iService = new Services.MaMapIndexService(new Data.MA.MACacheRepository(new Data.MA.MARepository(),
          new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.DfsMapIndexService iDfsService = new Services.DfsMapIndexService(new Data.DFS.DFSCacheRepository(new Data.DFS.DFSRepository(),
       new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.EpDataService iEpService = new Services.EpDataService(new Data.EP.EPCacheRepository(new Data.EP.EPRepository(),
    new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.PortfolioDataService iPortfolioService = new Services.PortfolioDataService(new Data.Portfolio.PortfolioCacheRepository(new Data.Portfolio.PortfolioRepository(),
                new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.LeafletService iLeafService =
            new Services.LeafletService(new LeafletHelpers.LeafletCacheScriptHelper(new LeafletHelpers.LeafletScriptHelper(),
          new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.MaQueryService iQueryService = new Services.MaQueryService(new QueryHelpers.QueryCacheHelper(new QueryHelpers.QueryHelper(),
                new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.ExportService iExport = new Services.ExportService(new Infrastructure.ExportHelpers.MA.MaCacheExportHelper(new Infrastructure.ExportHelpers.MA.MaExportHelpers(), new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.DfsExportService iDfsExport = new Services.DfsExportService(new Infrastructure.ExportHelpers.DFS.DfsCacheExportHelper(new Infrastructure.ExportHelpers.DFS.DfsExportHelpers(), new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.PortfolioExportService iPortExport = new Services.PortfolioExportService(new Infrastructure.ExportHelpers.Portfolio.PortfolioCacheExportHelper(new Infrastructure.ExportHelpers.Portfolio.PortfolioExportHelper(), 
            new Infrastructure.Caching.MemoryCacheProvider()));


        private Services.ChartingService iMaChart = 
            new Services.ChartingService(new ChartingData.MA.MaChartingCacheData(new ChartingData.MA.MaChartingData(), new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.MapIndexGeoserverService<IMapIndexData> iMaDataGeoService = new Services.MapIndexGeoserverService<IMapIndexData>(
    new GeoserverHelpers.MapIndex.MapIndexGeoserverCacheHelper<IMapIndexData>(new GeoserverHelpers.MapIndex.MapIndexGeoserverHelper<IMapIndexData>(),
      new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.MaViewHelperService iMaViewService = new Services.MaViewHelperService(
new GeoserverHelpers.MA.MaCacheClientHelper(new GeoserverHelpers.MA.MaClientHelpers(),
 new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.DfsViewHelperService iDfsViewService = new Services.DfsViewHelperService(
new GeoserverHelpers.DFS.DfsCacheClientHelper(new GeoserverHelpers.DFS.DfsClientHelpers(),
new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.PortfolioViewHelperService iPortViewService = new Services.PortfolioViewHelperService(
new GeoserverHelpers.Portfolio.PortfolioCacheClientHelper(new GeoserverHelpers.Portfolio.PortfolioClientHelpers(),
new Infrastructure.Caching.MemoryCacheProvider()));
        private Services.AuthenticationService iAuth =
            new Services.AuthenticationService(new Data.Authentication.IAuthCacheRepository(new Data.Authentication.AuthRepository(), 
                new Infrastructure.Caching.MemoryCacheProvider()));


        private Services.MapIndexService iMapService = new Services.MapIndexService();

        private Quickbase_API.Quickbase qApi = new Quickbase_API.Quickbase();

        private Infrastructure.EncodingHelpers.EncodingHelpers enc = new Infrastructure.EncodingHelpers.EncodingHelpers();

        private List<string> GetAutoComplete()
        {
            List<string> result = new List<string>();



            return result;
        }

        private object MaMarkerData(MapSearch map)
        {
            List<IMapIndexData> ma = iService.GetMaData();

            List<MaData> vals = CollectionHelpers.CollectionHelper<MaData>.GetProperList(ma);

            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);
            List<IMapIndexData> maWoutCompanies = new List<IMapIndexData>();

            SideFilterOptions sideFilters = new SideFilterOptions();
            List<string> autoComplete = new List<string>();
            if (map.Plays.Count > 0 || map.Regions.Count > 0 || map.Companies.Count > 0 || map.Entities.Count > 0 || !string.IsNullOrEmpty(map.TimeRange) || !string.IsNullOrEmpty(map.Keyword))
            {
                List<IMapIndexData> filteredList = iService.GetFilteredData(ma, map);
                vals = CollectionHelpers.CollectionHelper<MaData>.GetProperList(filteredList);
                List<MaData> withoutCompaniesSet = CollectionHelpers.CollectionHelper<MaData>.GetProperList(ma);

                if (!string.IsNullOrEmpty(map.Keyword) || map.Companies.Count > 0)
                {
                    maWoutCompanies = iService.GetDataBeforeCompanies(withoutCompaniesSet, map);
                }
                else
                {
                    maWoutCompanies = iService.GetDataBeforeCompanies(vals, map);
                }

                sideFilters = iService.GetSideFiltersMa(maWoutCompanies, map);                
                //ma = iService.GetFilteredData(maWoutCompanies, map);
                ma = filteredList;
                autoComplete = iService.GetKeywordAutoComplete(ma, map);
            }
            else
            {
                sideFilters = iService.GetSideFiltersMa(ma, map);
                autoComplete = iService.GetKeywordAutoComplete(ma, map);

            }


            List<MaData> maResult = CollectionHelpers.CollectionHelper<MaData>.GetProperList(ma);

            ChartingData.IChartOutput charts = iMaChart.GetChartOutput(maResult, map);

            Models.Charting.ChartOptions chartOptions = ((Models.Charting.ChartOptions)charts);

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
                companiesHtml = iMaDataGeoService.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath,map);
            }

            #region MyRegion
            //if (map.MaDealIds.Count > 0)
            //{
            //    checkListMaData = checkListMaData.Where(s => map.MaDealIds.Contains(s.id)).ToList();
            //}

            //else
            //{
            //    checkListMaData = vals2;
            //}
            #endregion

            if (map.Companies.Count > 0)
            {

            }
            Infrastructure.ExportHelpers.IExportData export = iExport.GetChecklistTable(maResult.ToList<Infrastructure.ExportHelpers.IExportData>(),map);

            Models.ExportData.ExportDataSet exportSet = ((Models.ExportData.ExportDataSet)export);


            #region MyRegion
            //List<MaExport> exportSetzz = new List<MaExport>();

            //foreach (var item in checkListMaData)
            //{
            //    macheckList.MaTable.Add(new MaCheckListTable(new List<string> { "Legend", "Announced", "Buyers", "Sellers", "Link", "GeoId" }, new List<string> { item.HexColor, item.Date_Announced.Value.ToString("MM-dd-yy"), item.Buyers, item.Sellers, item.id, item.theId }, new List<string> { "checkbox", "date", "text", "text", "link", "primarykey" }));
            //    exportSet.Add(new MaExport(item.id, item.Buyers, item.Sellers, item.Date_Announced, item.Value___MM_, item.__Acre, item.__Daily_BOE, item.Hydrocarbon, item.Headline, item.US_Region, item.US_Play, item.Lat, item.Long, item.DealType));
            //}

            //List<string> exportHeaders = new List<string>();
            //exportHeaders.Add("Record ID");
            //exportHeaders.Add("Buyers");
            //exportHeaders.Add("Sellers");
            //exportHeaders.Add("Date Announced");
            //exportHeaders.Add("Value ($MM)");
            //exportHeaders.Add("$/Acre");
            //exportHeaders.Add("$/Daily_BOE");
            //exportHeaders.Add("Hydrocarbon");
            //exportHeaders.Add("Headline");
            //exportHeaders.Add("US Region");
            //exportHeaders.Add("US Play");
            //exportHeaders.Add("Lat");
            //exportHeaders.Add("Long");
            //exportHeaders.Add("Deal Type");
            #endregion


            string cql = "";

            if (hasFilters && !string.IsNullOrEmpty(map.Keyword))
            {
                cql = iService.GeoserverCqlFilter(ma);
            }
            else if(hasFilters)
            {
                cql = GeoserverHelpers.GeoserverCqlFilter.CreateMACql(maResult, map);
            }

            string cacheKeyTable = "MALISTTABLE";

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

            var result = new
            {
                Result = iLeafService.GetMarkerData(maResult, new List<DfsData>(), new List<Models.EP.ViewModel.EpViewModel>(), map, "", false),
                SideFilters = sideFilters,
                //PortData = "",
                CheckSideListData = exportSet.ExportSet,
                TableData = maResult,
                CqlFilter = cql,
                ExportTableSet = exportSet.ExportSet,
                ExportHeaders = exportSet.ExportHeaders,
                ChartSeries = "",
                ChartX = chartOptions.ChartXCategories,
                ChartDataSeries = chartOptions.ChartSeries,
                CompanyHtml = companiesHtml,
                AutoCompleteSource = autoComplete,
                TableSource = iMaViewService.GenerateDataSetHtml(maResult, cacheKeyTable, "~/Views/Templates/MaTableList.cshtml", map)
                //DfsTableData = ""
            };

            return result;
        }

        [ActionName("GeneratePortfolioPopup")]
        [HttpPost]
        public IHttpActionResult GeneratePortfolioPopup(string CompanyId)
        {
            List<IMapIndexData> port = iPortfolioService.GetData();

            List<PortfolioData> vals = new List<PortfolioData>(port.Count);

            foreach (IMapIndexData d in port)
            {
                vals.Add((PortfolioData)d);
            }
            List<PortfolioData> checkListMaData = vals;

            PortfolioData p = vals.Where(s => s.CompanyId == CompanyId).FirstOrDefault();

            string popup = GeoserverHelpers.GeoserverPopupHelper.GeneratePopupPortfolio(p);

            var result = new
            {
                Lat = p.Latitude,
                Long = p.Longitude,
                Popup = popup
            };

            return Ok(result);
        }
     
        private object PortfolioMarkerData(MapSearch map)
        {
            List<IMapIndexData> port = iPortfolioService.GetData();
            List<Data.IMapIndexData> ep = iEpService.GetData();
            List<EpViewModel> epResults = CollectionHelpers.CollectionHelper<EpViewModel>.GetProperList(ep);
            List<PortfolioData> vals = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(port);

            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            //if (hasFilters)
            //{
            //    port = iPortfolioService.GetFilteredData(port, map);
            //}

            // port list before company
            List<IMapIndexData> maWoutCompanies = new List<IMapIndexData>();

            SideFilterOptions sideFilters = new SideFilterOptions();
            List<string> autoComplete = new List<string>();

            if (map.Plays.Count > 0 || map.Regions.Count > 0 || map.Entities.Count > 0 || !string.IsNullOrEmpty(map.Keyword) || map.Companies.Count > 0)
            {
                List<IMapIndexData> filteredList = iPortfolioService.GetFilteredData(port, map);
                vals = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(filteredList);
                List<PortfolioData> withoutCompaniesSet = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(port);
                if (!string.IsNullOrEmpty(map.Keyword) || map.Companies.Count > 0)
                {
                    maWoutCompanies = iPortfolioService.GetDataBeforeCompanies(withoutCompaniesSet, map);
                }
                else
                {
                    maWoutCompanies = iPortfolioService.GetDataBeforeCompanies(vals, map);
                }
                sideFilters = iPortfolioService.GetSideFilters(maWoutCompanies, map);

                if (map.Regions.Count > 0)
                {
                    epResults = epResults.Where(s => s.RegionId != null).Where(s => map.Regions.Contains(s.RegionId)).ToList();
                }
                port = filteredList;
                autoComplete = iPortfolioService.GetKeywordAutoComplete(port, map);

            }
            else
            {
                sideFilters = iPortfolioService.GetSideFilters(port, map);
                autoComplete = iPortfolioService.GetKeywordAutoComplete(port, map);

            }

            List<PortfolioData> portResult = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(port);

            //portResult = portResult.OrderBy(s => s.CompanyName).ToList();
          
            string companiesHtml = "";

            //if ((map.ReloadCompanies || map.InitialLoad))
            //{
            //    string cacheKey = ConfigurationFactory.Instance.Configuration().CacheKeyCompanyHtml + "_" + "PORTFOLIO";

            //    if (map.Plays.Count > 0)
            //    {
            //        cacheKey += "_" + "PLAYS" + string.Join(",", map.Plays);
            //    }
            //    if (map.Regions.Count > 0)
            //    {
            //        cacheKey += "_" + "REGIONS" + string.Join(",", map.Regions);

            //    }
               
            //    if (!string.IsNullOrEmpty(map.TimeRange))
            //    {
            //        cacheKey += "_" + "TIMERANGE" + map.TimeRange;

            //    }
            //    if (!string.IsNullOrEmpty(map.Keyword))
            //    {
            //        cacheKey += "_" + "KEYWORD" + map.Keyword;

            //    }
            //    companiesHtml = iMaDataGeoService.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath);

            //}
            string cacheKey = ConfigurationFactory.Instance.Configuration().CacheKeyCompanyHtml + "_" + map.WhichDb;

            if (map.Plays.Count > 0)
            {
                cacheKey += "_" + "PLAYS" + string.Join(",", map.Plays);
            }
            if (map.Regions.Count > 0)
            {
                cacheKey += "_" + "REGIONS" + string.Join(",", map.Regions);

            }

            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                cacheKey += "_" + "TIMERANGE" + map.TimeRange;

            }
            if (!string.IsNullOrEmpty(map.Keyword))
            {
                cacheKey += "_" + "KEYWORD" + map.Keyword;

            }
            companiesHtml = iMaDataGeoService.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath, map);
            Infrastructure.ExportHelpers.IExportData export = iPortExport.GetChecklistTable(portResult.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

            Models.ExportData.ExportDataSet exportSet = ((Models.ExportData.ExportDataSet)export);
            string cacheKeyTable = "PORTLISTTABLE";

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
            string cql = GeoserverHelpers.GeoserverCqlFilter.CreatePortfolioCql(portResult, map);
            PortfolioData p = new PortfolioData();
            string popup = "";

            if (map.Companies.Count > 0)
            {
                try
                {
                    p = portResult.Where(s => map.Companies.Last() == s.CompanyId).FirstOrDefault();
                    popup = GeoserverHelpers.GeoserverPopupHelper.GeneratePopupPortfolio(p);

                }
                catch (Exception ex)
                {

                }

            }

            if (p == null)
            {
                p = new PortfolioData();
                p.Latitude = "";
                p.Longitude = "";
            }
            if (popup == null)
            {
                popup = "";
            }
            
            var result = new
            {

                Result = iLeafService.GetMarkerData(new List<MaData>(), new List<DfsData>(), epResults.Take(500).ToList(), map, System.DateTime.Now.Date.ToString(), false),
                SideFilters = sideFilters,
                //PortData = "",
                CheckSideListData = exportSet.ExportSet,
                TableData = portResult,
                CqlFilter = cql,
                ExportTableSet = exportSet.ExportSet,
                ExportHeaders = exportSet.ExportHeaders,
                Popup = popup,
                Lat = p.Latitude,
                Long = p.Longitude,
                CompanyHtml = companiesHtml,
                AutoCompleteSource = autoComplete,
                TableSource = iPortViewService.GenerateDataSetHtml(portResult, cacheKeyTable, "~/Views/Templates/PortfolioTableList.cshtml", map)
                //DfsTableData = ""

            };

            return result;
        }

        private object DfsMarkerData(MapSearch map)
        {
            List<IMapIndexData> dfs = iDfsService.GetDfsData();
            List<DfsData> vals = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(dfs);

            string cql = "";


            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            //checklistDfsData = vals;
            List<IMapIndexData> maWoutCompanies = new List<IMapIndexData>();

            SideFilterOptions sideFilters = new SideFilterOptions();
            List<string> autoComplete = new List<string>();

            if (map.Plays.Count > 0 || map.Regions.Count > 0 || map.Companies.Count > 0 || map.Entities.Count > 0 || !string.IsNullOrEmpty(map.TimeRange) || !string.IsNullOrEmpty(map.Keyword))
            {
                List<IMapIndexData> filteredList = iDfsService.GetFilteredData(dfs, map);
                vals = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(filteredList);
                List<DfsData> withoutCompaniesSet = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(dfs);

                if (!string.IsNullOrEmpty(map.Keyword) || map.Companies.Count > 0)
                {
                    maWoutCompanies = iDfsService.GetDataBeforeCompanies(withoutCompaniesSet, map);
                }
                else
                {
                    maWoutCompanies = iDfsService.GetDataBeforeCompanies(vals, map);
                }

                sideFilters = iDfsService.GetSideFiltersDfs(maWoutCompanies, map);

                dfs = filteredList;
                autoComplete = iDfsService.GetKeywordAutoComplete(dfs, map);

            }
            else
            {
                sideFilters = iDfsService.GetSideFiltersDfs(dfs, map);
                autoComplete = iDfsService.GetKeywordAutoComplete(dfs, map);

            }


            List<DfsData> dfsResult = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(dfs);

            if (hasFilters && !string.IsNullOrEmpty(map.Keyword))
            {
                cql = iDfsService.GeoserverCqlFilter(dfs);
            }
            else if (hasFilters)
            {
                cql = GeoserverHelpers.GeoserverCqlFilter.CreateDFSCql(dfsResult, map);
            }


            #region MyRegion
            //if (map.ListingIds.Count > 0)
            //{
            //    checklistDfsData = vals2.Where(s => map.ListingIds.Contains(s.id)).ToList();
            //}

            //else
            //{
            //    checklistDfsData = vals2;
            //}
            #endregion
            string companiesHtml = "";

           
            string cacheKey = ConfigurationFactory.Instance.Configuration().CacheKeyCompanyHtml + "_" + map.WhichDb;

            if (map.Plays.Count > 0)
            {
                cacheKey += "_" + "PLAYS" + string.Join(",", map.Plays);
            }
            if (map.Regions.Count > 0)
            {
                cacheKey += "_" + "REGIONS" + string.Join(",", map.Regions);

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
            companiesHtml = iMaDataGeoService.GenerateCompanyHtml(sideFilters.Companies, cacheKey, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioCompaniesPath,map);
            Infrastructure.ExportHelpers.IExportData export = iDfsExport.GetChecklistTable(dfsResult.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

            Models.ExportData.ExportDataSet exportSet = ((Models.ExportData.ExportDataSet)export);
           
            var result = new
            {
                Result = iLeafService.GetMarkerData(new List<MaData>(), dfsResult, new List<Models.EP.ViewModel.EpViewModel>(), map, "", false),
                SideFilters = sideFilters,
                CqlFilter = cql,
                CheckSideListData = exportSet.ExportSet,
                TableData = dfsResult,
                ExportTableSet = exportSet.ExportSet,
                ExportHeaders = exportSet.ExportHeaders,
                CompanyHtml = companiesHtml,
                AutoCompleteSource = autoComplete,
                TableSource = iDfsViewService.GenerateDataSetHtml(dfsResult, cacheKeyTable, "~/Views/Templates/DfsTableList.cshtml", map)

            };
            return result;
        }

        private object EpMarkerData(MapSearch map)
        {
            List<Data.IMapIndexData> ep = iDfsService.GetDfsData();

            List<EpViewModel> vals = CollectionHelpers.CollectionHelper<EpViewModel>.GetProperList(ep);

            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            //checklistDfsData = vals;

            if (hasFilters)
            {
                //ep = iEpService.GetFilteredData(ep, map);
            }

            List<EpViewModel> vals2 = CollectionHelpers.CollectionHelper<EpViewModel>.GetProperList(ep);

            List<IMapIndexData> maWoutCompanies = new List<IMapIndexData>();

            SideFilterOptions sideFilters = new SideFilterOptions();

            if (map.Plays.Count > 0 || map.Regions.Count > 0 || map.Entities.Count > 0 || !string.IsNullOrEmpty(map.TimeRange))
            {
                //maWoutCompanies = iEpService.GetDataBeforeCompanies(vals, map);
                //sideFilters = iEpService.GetSideFiltersDfs(maWoutCompanies, map);

            }
            else
            {
                //sideFilters = iEpService.GetSideFiltersDfs(dfs, map);
            }

            #region MyRegion
            //if (map.ListingIds.Count > 0)
            //{
            //    checklistDfsData = vals2.Where(s => map.ListingIds.Contains(s.id)).ToList();
            //}

            //else
            //{
            //    checklistDfsData = vals2;
            //}
            #endregion
            string companiesHtml = "";

            if ((map.ReloadCompanies || map.InitialLoad))
            {
                companiesHtml = GeoserverHelpers.GeoserverPopupHelper.GenerateCompaniesLegendPortfolio(sideFilters.Companies);
            }

            //Infrastructure.ExportHelpers.IExportData export = iDfsExport.GetChecklistTable(vals2.ToList<Infrastructure.ExportHelpers.IExportData>(), map);

            //Models.ExportData.ExportDataSet exportSet = ((Models.ExportData.ExportDataSet)export);

            string cql = "";

            if (hasFilters && !string.IsNullOrEmpty(map.Keyword))
            {
                //cql = iDfsService.GeoserverCqlFilter(dfs);
            }
            else if (hasFilters)
            {
                cql = GeoserverHelpers.GeoserverCqlFilter.CreateCqlFilter(map);
            }

            var result = new
            {
                Result = iLeafService.GetMarkerData(new List<MaData>(), new List<DfsData>(), vals2, map, "", false),
                SideFilters = sideFilters,
                CqlFilter = cql,
                //CheckSideListData = exportSet.ExportSet,
                TableData = vals2,
                //ExportTableSet = exportSet.ExportSet,
                //ExportHeaders = exportSet.ExportHeaders,
                CompanyHtml = companiesHtml
            };
            return result;
        }



        // SESSION ID FOR EACH USER //
        // ONLY ON USER SPECIFIC STUFF //
        // qbToken + qbTicket + username --> FROM DATABASE
        [ActionName("GenerateMarkers")]
        [HttpPost]
        public IHttpActionResult GenerateMarkers(MapSearch map)
        {
            //map.TheSessionId = HttpContext.Current.Session.SessionID;
            Models.Authentication.ViewModels.AuthViewModel auth = Infrastructure.Session.AuthSession.GetAuthModel(map);

            map.TheSessionId = map.UserName + "_" + map.QbTicket + "_" + map.QbToken;
            
            if (iAuth.IsUserValid(auth))
            {

                object result = null;

                if (map.WhichDb == "DFSTAB")
                {
                    result = DfsMarkerData(map);
                }
                else if (map.WhichDb == "MATAB")
                {
                    result = MaMarkerData(map);
                }
                else if (map.WhichDb == "PORTFOLIOTAB")
                {
                    result = PortfolioMarkerData(map);
                }
                return Ok(result);
            }
            else
            {
                return Ok("Ok");
            }
            
         
        }

        public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
            {
                if (actionExecutedContext.Response != null)
                    actionExecutedContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                base.OnActionExecuted(actionExecutedContext);
            }
        }

        //[System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
        //[AllowCrossSiteJson]
        [ActionName("FetchTokenData")]
        [HttpGet]
        [System.Web.Http.Cors.EnableCors(origins: "*", headers: "*", methods: "*")]
        public IHttpActionResult FetchTokenData(Models.Authentication.ViewModels.AuthViewModel auth)
        {
            var response = new
            {
                Validates = iAuth.IsUserValid(auth),
                ticket = RandomGenerators.RandomGenerator.GenerateRandomWord(26),
                token = RandomGenerators.RandomGenerator.GenerateRandomWord(25)
            };

            return Ok(response);

        }
    }
}
