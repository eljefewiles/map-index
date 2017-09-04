using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MapIndex.Models.ViewModels;
using MapIndex.Models.DFS;
using MapIndex.Infrastructure.Caching;
using MapIndex.Models;
using System.Text;
using System.Data;

namespace MapIndex.Data.DFS
{
    public class DFSCacheRepository : IMapIndexDataRepository<DfsData>
    {
        private ICacheProvider _cacheProvider;
        private readonly IMapIndexDataRepository<DfsData> _commonRepository;

        public DFSCacheRepository(IMapIndexDataRepository<DfsData> commonRepository, ICacheProvider cacheProvider)
        {
            _commonRepository = commonRepository;
            _cacheProvider = cacheProvider;
        }

       

        public SideFilterOptions GetBaseSideFilters(List<IMapIndexData> data, MapSearch r)
        {
            //string cacheKey = "CACHE_KEY_SIDE_FILTERS_DFS_DEAL_COUNT_" + dfs.Count;
            StringBuilder cacheKey = new StringBuilder();
            //cacheKey.Append("CACHE_KEY_SIDE_FILTERS_DFS_DEAL_COUNT_").Append(dfs.Count);
            List<DfsData> dfs = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(data);
            DataTable companyDt = CompanyDataSet();
            DataTable regionDt = RegionDataSet();
            DataTable playDt = PlayDataSet();
            List<Company> company = GetBaseCompanies(dfs, r, companyDt);
            List<Play> play = GetBasePlays(dfs, r,playDt);
            List<Region> region = GetBaseRegions(dfs, r,regionDt);
            List<DealTypes> dealTypes = GetDealTypes(data, r);
            SideFilterOptions filters = new SideFilterOptions();
            filters.Companies = company;
            filters.Plays = play;
            filters.Regions = region;
            filters.DealTypes = dealTypes;
            
            return filters;
        }

        public List<Company> GetBaseCompanies(List<DfsData> dfs, MapSearch r, DataTable dt)
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append("CACHE_KEY_DFS_COMPANIES");
            if (r.Companies.Count == 0)
            {
                if (r.Regions != null && r.Regions.Count > 0)
                {
                    cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
                }
                if (r.Plays != null && r.Plays.Count > 0)
                {
                    cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
                }
                if (r.Entities != null && r.Entities.Count > 0)
                {
                    cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
                }
                if (!string.IsNullOrEmpty(r.TimeRange))
                {
                    cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
                }
                cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

                //if (r.ReloadCompanies)
                //{
                cacheKey.Append("_").Append("RELOADCOMPANIES").Append(r.ReloadCompanies.ToString());
                //}
                r.RefreshCompany = "";
            }

            else if (r.Companies.Count > 0)
            {
                if (r.Regions != null && r.Regions.Count > 0)
                {
                    cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
                }
                if (r.Plays != null && r.Plays.Count > 0)
                {
                    cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
                }
                if (r.Entities != null && r.Entities.Count > 0)
                {
                    cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
                }
                if (!string.IsNullOrEmpty(r.TimeRange))
                {
                    cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
                }
                if (r.ReloadCompanies == false)
                {
                    cacheKey.Append("_").Append("RELOADCOMPANIES").Append(true.ToString());
                }
                else
                {
                    cacheKey.Append("_").Append("RELOADCOMPANIES").Append(r.ReloadCompanies.ToString());
                }
                cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

                r.RefreshCompany = "true";
            }

            var items = _cacheProvider.Retrieve<List<Company>>(cacheKey.ToString());

            if (items == null || !items.Any())
            {
                items = _commonRepository.GetBaseCompanies(dfs, r,dt);

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }

        public List<DealTypes> GetDealTypes(List<IMapIndexData> dfs, MapSearch r)
        {

            StringBuilder cacheKey = new StringBuilder();

            cacheKey.Append("CACHE_KEY_DFS_DEALTYPES");

            if (r.Companies != null && r.Companies.Count > 0)
            {
                cacheKey.Append("_").Append("COMPANIES").Append("_").Append(string.Join(",", r.Companies));
            }
            if (r.Regions != null && r.Regions.Count > 0)
            {
                cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
            }
            if (r.Plays != null && r.Plays.Count > 0)
            {
                cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
            }
            if (r.Entities != null && r.Entities.Count > 0)
            {
                cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
            }
            if (!string.IsNullOrEmpty(r.TimeRange))
            {
                cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
            }
            if (!string.IsNullOrEmpty(r.Keyword))
            {
                cacheKey.Append("_").Append("KEYWORD").Append("_").Append(r.Keyword);
            }
            cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

            var items = _cacheProvider.Retrieve<List<DealTypes>>(cacheKey.ToString());

            if (items == null || !items.Any())
            {
                items = _commonRepository.GetDealTypes(dfs, r);

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }

        string CacheKeyDfsData = "DFSDATACACHEKEY";

        public List<IMapIndexData> GetData()
        {
            string cacheKey = Infrastructure.Configuration.ConfigurationFactory.Instance.Configuration().CacheKeyBaseDfsData;

            var items = _cacheProvider.Retrieve<List<IMapIndexData>>(cacheKey);

            if (items == null || !items.Any())
            {
                items = _commonRepository.GetData();

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey, items, int.MaxValue);
                }
            }
            return items;

        }

        public List<Play> GetBasePlays(List<DfsData> data, MapSearch r, DataTable dt)
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append("CACHE_KEY_DFS_PLAYS");
            
            if (r.Companies != null && r.Companies.Count > 0)
            {
                cacheKey.Append("_").Append("COMPANIES").Append("_").Append(string.Join(",", r.Companies));
            }
            if (r.Plays != null && r.Plays.Count > 0)
            {
                cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
            }
            if (r.Regions != null && r.Regions.Count > 0)
            {
                cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
            }
            if (r.Entities != null && r.Entities.Count > 0)
            {
                cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
            }
            if (!string.IsNullOrEmpty(r.TimeRange))
            {
                cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
            }
            cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

            var items = _cacheProvider.Retrieve<List<Play>>(cacheKey.ToString());

            if (items == null || !items.Any())
            {
                items = _commonRepository.GetBasePlays(data, r,dt);

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }

        public List<Region> GetBaseRegions(List<DfsData> data, MapSearch r, DataTable dt)
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append("CACHE_KEY_DFS_REGIONS");
            
            if (r.Companies != null && r.Companies.Count > 0)
            {
                cacheKey.Append("_").Append("COMPANIES").Append("_").Append(string.Join(",", r.Companies));
            }
            if (r.Plays != null && r.Plays.Count > 0)
            {
                cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
            }
            if (r.Regions != null && r.Regions.Count > 0)
            {
                cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
            }
            if (r.Entities != null && r.Entities.Count > 0)
            {
                cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
            }
            if (!string.IsNullOrEmpty(r.TimeRange))
            {
                cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
            }
            cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

            var items = _cacheProvider.Retrieve<List<Region>>(cacheKey.ToString());

            if (items == null || !items.Any())
            {
                items = _commonRepository.GetBaseRegions(data, r,dt);

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }

        public List<Models.SourceDoc> GetSourceMapDoc()
        {
            return null;
        }

        public List<IMapIndexData> GetFilteredData(List<DfsData> data, MapSearch r)
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append("CACHE_KEY_DFS_FILTERED_DATA");
            if (r.Companies != null && r.Companies.Count > 0)
            {
                cacheKey.Append("_").Append("COMPANIES").Append("_").Append(string.Join(",", r.Companies));
            }
            if (r.Plays != null && r.Plays.Count > 0)
            {
                cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
            }
            if (r.Regions != null && r.Regions.Count > 0)
            {
                cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
            }
            if (r.Entities != null && r.Entities.Count > 0)
            {
                cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
            }
            if (!string.IsNullOrEmpty(r.TimeRange))
            {
                cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
            }
            if (!string.IsNullOrEmpty(r.Keyword))
            {
                cacheKey.Append("_").Append("KEYWORD").Append("_").Append(r.Keyword);
            }
            cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

            var items = _cacheProvider.Retrieve<List<IMapIndexData>>(cacheKey.ToString());

            if (items == null || !items.Any())
            {
                items = _commonRepository.GetFilteredData(data, r);

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }

        public string GeoserverCqlFilter(List<string> ids)
        {
            return _commonRepository.GeoserverCqlFilter(ids);
        }

        public List<IMapIndexData> DataBeforeCompanies(List<DfsData> data, MapSearch r)
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append("CACHE_KEY_DFS_DATA_BEFORE_COMPANIES");
            if (r.Plays != null && r.Plays.Count > 0)
            {
                cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
            }
            if (r.Regions != null && r.Regions.Count > 0)
            {
                cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
            }
            if (r.Entities != null && r.Entities.Count > 0)
            {
                cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
            }
            if (!string.IsNullOrEmpty(r.TimeRange))
            {
                cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
            }
            if (!string.IsNullOrEmpty(r.Keyword))
            {
                cacheKey.Append("_").Append("KEYWORD").Append("_").Append(r.Keyword);
            }
            cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

            var items = _cacheProvider.Retrieve<List<IMapIndexData>>(cacheKey.ToString());

            if (items == null || !items.Any())
            {
                items = _commonRepository.DataBeforeCompanies(data, r);

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }

        public List<string> GetKeywordAutoComplete(List<IMapIndexData> data, MapSearch r)
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append("CACHE_KEY_DFS_AUTOCOMPLETE_DATA");
            if (r.Companies != null && r.Companies.Count > 0)
            {
                cacheKey.Append("_").Append("COMPANIES").Append("_").Append(string.Join(",", r.Companies));
            }
            if (r.Plays != null && r.Plays.Count > 0)
            {
                cacheKey.Append("_").Append("PLAYS").Append("_").Append(string.Join(",", r.Plays));
            }
            if (r.Regions != null && r.Regions.Count > 0)
            {
                cacheKey.Append("_").Append("REGIONS").Append("_").Append(string.Join(",", r.Regions));
            }
            if (r.Entities != null && r.Entities.Count > 0)
            {
                cacheKey.Append("_").Append("DEALTYPES").Append("_").Append(string.Join(",", r.Entities));
            }
            if (!string.IsNullOrEmpty(r.TimeRange))
            {
                cacheKey.Append("_").Append("TIMERANGE").Append("_").Append(r.TimeRange);
            }
            if (!string.IsNullOrEmpty(r.Keyword))
            {
                cacheKey.Append("_").Append("KEYWORD").Append("_").Append(r.Keyword);
            }
            cacheKey.Append("_").Append("SESSIONID").Append(r.TheSessionId);

            var items = _cacheProvider.Retrieve<List<string>>(cacheKey.ToString());

            if (items == null || !items.Any())
            {
                items = _commonRepository.GetKeywordAutoComplete(data, r);

                if (items.Any())
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;

        }

        public DataTable CompanyDataSet()
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append(Infrastructure.Configuration.ConfigurationFactory.Instance.Configuration().CacheKeyDfsCompanyDataSet);
            var items = _cacheProvider.Retrieve<DataTable>(cacheKey.ToString());
            if (items == null || items.Rows.Count == 0)
            {
                items = _commonRepository.CompanyDataSet();

                if (items.Rows.Count > 0)
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;

        }

        public DataTable RegionDataSet()
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append(Infrastructure.Configuration.ConfigurationFactory.Instance.Configuration().CacheKeyDfsRegionDataSet);
            var items = _cacheProvider.Retrieve<DataTable>(cacheKey.ToString());
            if (items == null || items.Rows.Count == 0)
            {
                items = _commonRepository.RegionDataSet();

                if (items.Rows.Count > 0)
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }

        public DataTable PlayDataSet()
        {
            StringBuilder cacheKey = new StringBuilder();
            cacheKey.Append(Infrastructure.Configuration.ConfigurationFactory.Instance.Configuration().CacheKeyDfsPlayDataSet);
            var items = _cacheProvider.Retrieve<DataTable>(cacheKey.ToString());
            if (items == null || items.Rows.Count == 0)
            {
                items = _commonRepository.PlayDataSet();

                if (items.Rows.Count > 0)
                {
                    _cacheProvider.Store(cacheKey.ToString(), items, int.MaxValue);
                }
            }
            return items;
        }
    }
}