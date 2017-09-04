using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MapIndex.Models;
using MapIndex.Models.DFS;
using MapIndex.Models.ViewModels;
using Dapper;
using System.Configuration;
using MapIndex.Infrastructure.Logging;
using System.Text;

using System.Linq.Dynamic;
using System.Data;

namespace MapIndex.Data.DFS
{
    public class DFSRepository : BaseRepository<DfsData>, IMapIndexDataRepository<DfsData>
    {

        List<IMapIndexData> IMapIndexDataRepository<DfsData>.GetData()
        {

            List<IMapIndexData> retVal = new List<IMapIndexData>();

            List<Models.SourceDoc> sourceDocs = GetSourceMapDoc();

            string query = @"SELECT l.id, l.theId, l.Headline, case when l.AgentId IS NOT NULL THEN c.image ELSE '' END AS AgentLogo, 
case when l.AgentId IS NULL THEN '0' ELSE l.AgentId END AS AgentId, 
case when l.SellerId IS NOT NULL THEN cl.image ELSE '' END AS SellerLogo, 
case when l.SellerId IS NULL THEN '0' ELSE l.SellerId END AS SellerId, 
case when l.Seller IS NULL THEN '' ELSE l.Seller END AS Sellers, 
case when l.Agent_Broker IS NULL THEN '' else l.Agent_Broker END AS Agent_Broker, 
l.ListingCode AS PLS_Code, l.RegionName AS US_Region, l.RegionId,
l.PlayName AS US_Play, l.PlayId, l.DBID, l.Icon, l.DealType, l.DealTypeId, 
l.Lat, l.Long, l.Listing_Date, case when l.HexColor IS NULL THEN 'blue' ELSE l.HexColor END AS HexColor 
FROM [MapIndex].[dbo].[listingGeometryView] AS l
LEFT OUTER JOIN PLSX_Production.dbo.tblCompany c ON c.id = l.AgentId
LEFT OUTER JOIN PLSX_Production.dbo.tblCompany cl ON cl.id = l.SellerId
ORDER BY Listing_Date DESC";

            List<IMapIndexData> results = GetData(query);

            List<DfsData> vals = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(results);

            foreach (var item in vals)
            {
                item.SourceDocs = sourceDocs.Where(s => s.Id == item.id).ToList();
            }

            return results;
        }

        List<Play> IMapIndexDataRepository<DfsData>.GetBasePlays(List<DfsData> data, MapSearch map, DataTable dt)
        {
            return GetBasePlays(data, map,dt);
        }

        List<Region> IMapIndexDataRepository<DfsData>.GetBaseRegions(List<DfsData> data, MapSearch map, DataTable dt)
        {
            return GetBaseRegions(data, map,dt);
        }

        public SideFilterOptions GetBaseSideFilters(List<IMapIndexData> dfs, MapSearch map)
        {
            SideFilterOptions sideFilter = new SideFilterOptions();
            List<DfsData> vals = GetProperList(dfs);
            DataTable companyDt = CompanyDataSet();
            DataTable regionDt = RegionDataSet();
            DataTable playDt = PlayDataSet();

            sideFilter.Plays = GetBasePlays(vals, map, playDt);
            sideFilter.Companies = GetBaseCompanies(vals, map, companyDt);
            sideFilter.Regions = GetBaseRegions(vals, map, regionDt);
            sideFilter.DealTypes = GetDealTypes(dfs, map);

            return sideFilter;
        }

        public List<DealTypes> GetDealTypes(List<IMapIndexData> dfs, MapSearch map)
        {
            List<DealTypes> dealTypes = new List<DealTypes>();
            string query = @"SELECT dealtypeid, DealType FROM [MapIndex].[dbo].[DealType] WHERE IsDfs = 1 ORDER BY DealType ASC";

            System.Data.DataTable dt = GetDataTable(query);
            List<DfsData> vals = GetProperList(dfs);

            foreach (System.Data.DataRow dr in dt.Rows)
            {
                DealTypes dealType = new DealTypes();

                try
                {
                   dealType.DealCount = vals.Where(s => s.DealTypeId == dr["dealtypeid"].ToString()).Count();

                }
                catch (Exception e)
                {

                }

                dealType.DealType = dr["DealType"].ToString();
                dealType.Id = dr["dealtypeid"].ToString();
                //if (dealType.DealCount > 0)
                //{
                dealTypes.Add(dealType);
                //}
            }

            return dealTypes;

        }

        List<Company> IMapIndexDataRepository<DfsData>.GetBaseCompanies(List<DfsData> data, MapSearch map, DataTable dt)
        {
            List<Company> companies = new List<Company>();
            foreach (System.Data.DataRow dr in dt.Rows)
            {
                Company company = new Company();

                try
                {
                    company.DealCount = data.Where(s => s.AgentId != null && s.SellerId != null)
                        .Where(s => s.AgentId.Contains(dr["companyId"].ToString()) || s.SellerId.Contains(dr["companyId"].ToString())).Count();

                }
                catch (Exception e)
                {

                }
                company.CompanyId = dr["companyId"].ToString();
                company.CompanyName = dr["companyName"].ToString();
                company.Id = Convert.ToInt32(dr["companyId"]);

                if (company.DealCount > 0)
                {
                    companies.Add(company);
                }

            }

            return companies;
        }
   
        internal override List<IMapIndexData> GetPlayListData(List<DfsData> data, List<string> filterIds)
        {

            List<string> ids = new List<string>();
            foreach (var item4 in filterIds)
            {
                foreach (var item in data)
                {
                    try
                    {
                        foreach (var item2 in item.PlayId.Split(','))
                        {
                            if (item2 == item4)
                            {
                                ids.Add(item.id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
            data = data.Where(s => ids.Contains(s.id)).ToList();
            List<IMapIndexData> resultsBeforeCompanies = new List<IMapIndexData>();

            foreach (DfsData d in data)
            {
                resultsBeforeCompanies.Add((IMapIndexData)d);
            }

            return resultsBeforeCompanies;
        }

        internal override List<IMapIndexData> GetRegionListData(List<DfsData> data, List<string> filterIds)
        {

            List<string> ids = new List<string>();
            foreach (var item4 in filterIds)
            {
                foreach (var item in data)
                {
                    try
                    {
                        foreach (var item2 in item.RegionId.Split(','))
                        {
                            if (item2 == item4)
                            {
                                ids.Add(item.id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
            data = data.Where(s => ids.Contains(s.id)).ToList();
            List<IMapIndexData> resultsBeforeCompanies = new List<IMapIndexData>();

            foreach (DfsData d in data)
            {
                resultsBeforeCompanies.Add((IMapIndexData)d);
            }

            return resultsBeforeCompanies;

        }

        internal override List<IMapIndexData> GetDealTypeListData(List<DfsData> data, List<string> filterIds)
        {

            List<string> ids = new List<string>();
            foreach (var item4 in filterIds)
            {
                foreach (var item in data)
                {
                    try
                    {
                        foreach (var item2 in item.DealTypeId.Split(','))
                        {
                            if (item2 == item4)
                            {
                                ids.Add(item.id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }

            data = data.Where(s => ids.Contains(s.id)).ToList();

            List<IMapIndexData> resultsBeforeCompanies = new List<IMapIndexData>();

            foreach (DfsData d in data)
            {
                resultsBeforeCompanies.Add((IMapIndexData)d);
            }

            return resultsBeforeCompanies;
        }

        List<IMapIndexData> IMapIndexDataRepository<DfsData>.DataBeforeCompanies(List<DfsData> data, MapSearch map)
        {
            List<IMapIndexData> results = new List<IMapIndexData>();
            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                try
                {
                    DateTime dateVal = DateTime.Now.AddDays(-+Convert.ToInt32(map.TimeRange));

                    data = data.Where(s => s.Listing_Date >= dateVal).ToList();
                }
                catch (Exception ex)
                {

                }
            }

            List<IMapIndexData> playsData = GetPlayListData(data, map.Plays);
            List<IMapIndexData> regionData = GetRegionListData(data, map.Regions);
            List<IMapIndexData> dealTypeData = GetDealTypeListData(data, map.Entities);

            if (playsData.Count > 0 && regionData.Count > 0 && dealTypeData.Count > 0)
            {
                results = playsData.Concat(regionData).Concat(dealTypeData).ToList();
            }
            else if (playsData.Count == 0 && regionData.Count > 0 && dealTypeData.Count == 0)
            {
                results = regionData;
            }
            else if (playsData.Count > 0 && regionData.Count == 0 && dealTypeData.Count == 0)
            {
                results = playsData;
            }
            else if (playsData.Count == 0 && regionData.Count == 0 && dealTypeData.Count > 0)
            {
                results = dealTypeData;
            }
            else if (playsData.Count > 0 && regionData.Count > 0 && dealTypeData.Count == 0)
            {
                results = playsData.Concat(regionData).ToList();
            }
            else if (playsData.Count == 0 && regionData.Count > 0 && dealTypeData.Count == 0)
            {
                results = regionData.Concat(dealTypeData).ToList();
            }
            else if (playsData.Count > 0 && regionData.Count == 0 && dealTypeData.Count > 0)
            {
                results = playsData.Concat(dealTypeData).ToList();
            }
            if (playsData.Count == 0 && regionData.Count == 0 && dealTypeData.Count == 0)
            {
                if (!string.IsNullOrEmpty(map.TimeRange) || !string.IsNullOrEmpty(map.Keyword))
                {
                    results = GetNonProperList(data);
                }
                //if (!string.IsNullOrEmpty(map.Keyword))
                //{
                //    results = GetNonProperList(data);
                //}
            }
            return results;
        }

        public List<Models.SourceDoc> GetSourceMapDoc()
        {
            List<Models.SourceDoc> sourceDocs = new List<Models.SourceDoc>();

            using (var con = base.GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
            {
                con.Open();
                string query = @"SELECT listing_id AS Id, [File] AS SourceDocument, Quick_Comment AS QuickComment, Document_Type AS DocumentType FROM MapIndex.dbo.[bjquik4qj]
                                        WHERE listing_id IS NOT NULL AND (Document_Type = '" + "ShapeFile" + "' OR Document_Type = '" + "Map" + "')";
                sourceDocs = con.Query<Models.SourceDoc>(query).ToList();
            }

            return sourceDocs;
        }

        internal override List<IMapIndexData> CompanySearch(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            StringBuilder sb = new StringBuilder();
            List<DfsData> dfs = GetProperList(data);

            if (map.Companies != null)
            {
                if (map.Companies.Count > 0)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(map.Companies[0].ToString()))
                        {
                            dfs = dfs.Where(s => map.Companies.Contains(s.AgentId) || map.Companies.Contains(s.SellerId)).ToList();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    //ma = ma.Where(s => map.Companies.Contains(s.BuyerIds) || map.Companies.Contains(s.SellerIds)).ToList();
                }
            }
           

            return dfs.ToList<IMapIndexData>();
        }

        internal override List<string> GetCompanyFilters(List<string> theQueryString, MapSearch map)
        {
            List<string> andQueryList = new List<string>();

            if (map.Companies != null)
            {
                if (map.Companies.Count > 0)
                {
                    if (theQueryString.Count > 0)
                    {
                        andQueryList.AddRange(BuildContainsQuery("AgentId", map.Companies));
                        andQueryList.AddRange(BuildContainsQuery("SellerId", map.Companies));

                    }
                    else
                    {
                        theQueryString.AddRange(BuildContainsQuery("AgentId", map.Companies));
                        theQueryString.AddRange(BuildContainsQuery("SellerId", map.Companies));
                    }
                }
            }
          
            return theQueryString;
        }

        internal override List<IMapIndexData> KeywordSearch(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            List<DfsData> dfs = GetProperList(data);
            if (!string.IsNullOrEmpty(map.Keyword))
            {
                dfs = dfs.Where(s => s.US_Play.ToLower().Contains(map.Keyword.ToLower()) || s.US_Region.ToLower().Contains(map.Keyword.ToLower()) || s.Sellers.ToLower().Contains(map.Keyword.ToLower()) || s.Agent_Broker.ToLower().Contains(map.Keyword.ToLower()) || s.DealType.ToLower().Contains(map.Keyword.ToLower())).ToList();
            }
            return dfs.ToList<IMapIndexData>();
        }

        List<IMapIndexData> IMapIndexDataRepository<DfsData>.GetFilteredData(List<DfsData> data, MapSearch map)
        {
            return GetFilteredData(data, map);
        }

        internal override List<string> GetRecordIdFilters(List<string> theQueryString, MapSearch map)
        {

            if (map.ListingIds.Count > 0)
            {
                theQueryString.AddRange(BuildEqualQuery("id", map.ListingIds));
            }
            return theQueryString;
        }

        internal override List<IMapIndexData> TimeRange(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            StringBuilder sb = new StringBuilder();

            List<DfsData> dfs = GetProperList(data);
            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                DateTime dateVal = DateTime.Now.AddDays(-+Convert.ToInt32(map.TimeRange));
                dfs = dfs.Where(s => s.Listing_Date >= dateVal).ToList();

               
            }

            return dfs.ToList<IMapIndexData>();
        }

        internal override List<IMapIndexData> QueryData(List<DfsData> data, MapSearch map, string query)
        {
            List<IMapIndexData> retVal = new List<IMapIndexData>();

            data = data.Where(s => s.SellerId != null && s.AgentId != null).Where(query).ToList();
            data = data.Select(s => s).Distinct().ToList();
            retVal = data.ToList<IMapIndexData>();

            if (!string.IsNullOrEmpty(map.TimeRange))
            {
                retVal = TimeRange("", retVal, map);
            }

            if (!string.IsNullOrEmpty(map.Keyword))
            {
                retVal = KeywordSearch("", retVal, map);
            }
            if (map.Companies.Count > 0)
            {
                retVal = CompanySearch("", retVal, map);
            }

            return retVal;
        }

        public string GeoserverCqlFilter(List<string> ids)
        {
            return base.GeoserverCql(ids);
        }

        List<string> IMapIndexDataRepository<DfsData>.GetKeywordAutoComplete(List<IMapIndexData> data, MapSearch map)
        {
            return GetKeywordAutoComplete(data, map);
        }
        public DataTable CompanyDataSet()
        {
            DataTable dt = new DataTable();

            dt = GetDataTable(CompanyQuery());

            return dt;
        }

        public DataTable RegionDataSet()
        {
            DataTable dt = new DataTable();

            dt = GetDataTable(RegionQuery());

            return dt;
        }

        public DataTable PlayDataSet()
        {
            DataTable dt = new DataTable();

            dt = GetDataTable(PlayQuery());

            return dt;
        }
    }
}