using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using Dapper;
using System.Configuration;
using System.Data;
using System.Net.Mail;
using MapIndex.Infrastructure.Logging;
using MapIndex.Models.MA;
using MapIndex.Models.ViewModels;
using System.Text;
using System.Linq.Dynamic;
using MapIndex.Models;
using MapIndex.Models.DFS;
using MapIndex.Models.Portfolio;

namespace MapIndex.Data
{
    public abstract class BaseRepository<T>
    {
        public IDbConnection GetConnection(string conStr)
        {
            return new SqlConnection(conStr);
        }

        public virtual string ConnectionString { get; }

        public DataTable GetDataTable(string query)
        {
            DataTable dt = new DataTable();

            try
            {
                using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
                {
                    con.Open();
                    dt.Load(con.ExecuteReader(query));
                }
            }
            catch (Exception ex)
            {
                LogFactory.Instance.Log().WriteMessage("", ex);
            }

            return dt;
        }

        internal List<string> BuildEqualQuery(string propertyName, List<string> ids)
        {
            List<string> list = new List<string>();

            foreach (string item in ids)
            {

                foreach (string splitItem in item.Split(','))
                {
                    if (!string.IsNullOrEmpty(splitItem))
                    {
                        string query = propertyName + " = " + "\"" + splitItem + "\"";
                        list.Add(query);
                    }
                  
                }
            }

            return list;
        }       

        internal virtual List<IMapIndexData> GetPlayListData(List<T> data, List<string> filterIds)
        {
            List<IMapIndexData> results = new List<IMapIndexData>();         
            return results;
        }
        internal virtual List<IMapIndexData> GetRegionListData(List<T> data, List<string> filterIds)
        {
            List<IMapIndexData> results = new List<IMapIndexData>();
            return results;
        }
        internal virtual List<IMapIndexData> GetDealTypeListData(List<T> data, List<string> filterIds)
        {
            List<IMapIndexData> results = new List<IMapIndexData>();
            
            return results;
        }
        protected List<IMapIndexData> DataBeforeCompanies(List<T> data, MapSearch map)
        {
            List<IMapIndexData> results = new List<IMapIndexData>();
          


            return results;
        }

        internal virtual List<IMapIndexData> GetData(string query)
        {
            List<IMapIndexData> retVal = new List<IMapIndexData>();

            try
            {
                using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
                {
                    con.Open();

                    var s = con.Query<T>(query).ToList();
                    retVal = GetNonProperList(s);
                }
            }
            catch (Exception ex)
            {
                LogFactory.Instance.Log().WriteMessage("Error getting Ma Data: " + ex.Message, ex);
            }

            return retVal;
        }

        internal string PlayQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ").AppendLine();
            sb.Append(" id ").Append(" AS ").Append(" id ").Append(", ").AppendLine();
            sb.Append(" PlayName ").Append(" AS ").Append(" PlayName ").AppendLine();
            sb.Append(" FROM [MapIndex].[dbo].[PlayGeometry]").AppendLine();
            return sb.ToString();
        }

        internal string RegionQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ").AppendLine();
            sb.Append(" regionid ").Append(" AS ").Append(" regionid ").Append(", ").AppendLine();
            sb.Append(" RegionName ").Append(" AS ").Append(" RegionName ").AppendLine();
            sb.Append(" FROM [MapIndex].[dbo].[RegionGeometry]").AppendLine();
            return sb.ToString();
        }

        internal string CompanyQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT companyId, companyName FROM (SELECT ROW_NUMBER() OVER(PARTITION BY companyId ORDER BY companyId ASC) AS rn, companyId, companyName FROM [MapIndex].[synch].[company] WHERE companyName <> '' AND companyName IS NOT NULL) t WHERE rn = 1");
            return sb.ToString();
        }

        protected List<Play> GetBasePlays(List<T> data, MapSearch map, DataTable dt)
        {
            List<Play> plays = new List<Play>();


            StringBuilder whereClause = new StringBuilder();

            foreach (System.Data.DataRow dr in dt.Rows)
            {
                Play play = new Play();

                play.PlayName = dr["PlayName"].ToString();


                if (map.WhichDb == "PORTFOLIOTAB")
                {
                    whereClause.Append("US_Play.Contains(" + "\"" + dr["PlayName"].ToString() + "\"" + ")");
                }
                else if (map.WhichDb == "MATAB")
                {
                    whereClause.Append("PlayId != null AND PlayId " + " = " + "\"" + dr["id"].ToString() + "\"");
                }
                else if (map.WhichDb == "DFSTAB")
                {
                    whereClause.Append("PlayId != null AND PlayId " + " = " + "\"" + dr["id"].ToString() + "\"");
                }
                try
                {
                    //play.LayerGroupName = MaPlayLayerGroups()[dr["PlayName"].ToString()];
                    play.DealCount = data.Where(whereClause.ToString()).Count();

                }
                catch (Exception ex)
                {
                    LogFactory.Instance.Log().WriteMessage("Error Getting Map Queries: " + ex.Message);
                }

                play.IsSelected = false;
                play.Id = dr["id"].ToString();
                plays.Add(play);
                whereClause.Clear();
                
            }

            return plays;

        }

        protected List<Region> GetBaseRegions(List<T> data, MapSearch map, DataTable dt)
        {
            List<Region> regions = new List<Region>();


            StringBuilder whereClause = new StringBuilder();

            foreach (System.Data.DataRow dr in dt.Rows)
            {
                Region region = new Region();

                region.RegionName = dr["RegionName"].ToString();


                if (map.WhichDb == "PORTFOLIOTAB")
                {
                    whereClause.Append("US_Region.Contains(" + "\"" + dr["RegionName"].ToString() + "\"" + ")");
                }
                else if (map.WhichDb == "MATAB")
                {
                    whereClause.Append("RegionId != null AND RegionId " + " = " + "\"" + dr["regionid"].ToString() + "\"");
                }
                else if (map.WhichDb == "DFSTAB")
                {
                    whereClause.Append("RegionId != null AND RegionId " + " = " + "\"" + dr["regionid"].ToString() + "\"");
                }
                try
                {
                    //play.LayerGroupName = MaPlayLayerGroups()[dr["PlayName"].ToString()];
                    region.DealCount = data.Where(whereClause.ToString()).Count();

                }
                catch (Exception ex)
                {
                    LogFactory.Instance.Log().WriteMessage("Error Getting Map Queries: " + ex.Message);
                }

                region.IsSelected = false;
                region.Id = dr["regionid"].ToString();
                regions.Add(region);
                whereClause.Clear();

            }

            return regions;

        }
       
        internal virtual List<Company> GetBaseCompanies(List<T> data, MapSearch map, DataTable dt)
        {
            List<Company> companies = new List<Company>();

            return companies;
        }

        internal List<string> BuildIntersectQuery(string propertyName, List<string> ids)
        {
            List<string> list = new List<string>();

            foreach (string item in ids)
            {
                foreach (string splitItem in item.Split(','))
                {
                    if (!string.IsNullOrEmpty(splitItem))
                    {
                        string query = propertyName + ".Intersects(" + splitItem + ")";
                        list.Add(query);
                    }
                }
            }

            return list;
        }

        internal string BuildCqlIn(string propertyName, List<string> ids)
        {
            string query = "";
            if (ids.Count > 0)
            {
                query = propertyName + " IN('" + string.Join("','", ids) + "')";
            }
           
            return query;
        }

        internal List<string> BuildContainsQuery(string propertyName, List<string> ids)
        {
            List<string> list = new List<string>();

            foreach (string item in ids)
            {
                foreach (string splitItem in item.Split(','))
                {
                    if (!string.IsNullOrEmpty(splitItem))
                    {
                        string query = propertyName + ".Contains(" + "\"" + splitItem + "\")";
                        list.Add(query);
                    }
                }
            }

            return list;
        }

        internal List<IMapIndexData> FilterDataSet(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            List<IMapIndexData> vals = new List<IMapIndexData>(data.Count);

            foreach (T d in data)
            {
                vals.Add((IMapIndexData)d);
            }
            return vals;
        }

        internal List<T> GetProperList(List<IMapIndexData> data)
        {
            List<T> vals = new List<T>(data.Count);

            foreach (IMapIndexData d in data)
            {
                vals.Add((T)d);
            }
            return vals;
        }

        internal List<IMapIndexData> GetNonProperList(List<T> data)
        {
            List<IMapIndexData> vals = new List<IMapIndexData>(data.Count);

            foreach (T d in data)
            {
                vals.Add((IMapIndexData)d);
            }
            return vals;
        }

        internal virtual List<IMapIndexData> TimeRange(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            return data;
        }

        internal virtual List<IMapIndexData> CompanySearch(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            return data;
        }

        internal virtual List<IMapIndexData> KeywordSearch(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            return data;
        }

        internal virtual List<IMapIndexData> PlaySearch(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            return data;
        }
        internal virtual List<IMapIndexData> RegionSearch(string theQueryString, List<IMapIndexData> data, MapSearch map)
        {
            return data;
        }

        internal virtual string GeoserverCql(List<string> data)
        {
            string retVal = "";

            retVal = BuildCqlIn("theId", data);

            return retVal;
        }

        protected List<string> GetKeywordAutoComplete(List<IMapIndexData> data, MapSearch map)
        {
            List<string> list = new List<string>();

            if (map.WhichDb == "MATAB")
            {
                List<MaData> results = CollectionHelpers.CollectionHelper<MaData>.GetProperList(data);
                foreach (var item in results.Select(s => s.Buyers.Trim()))
                {
                    foreach (string splitItem in item.Split(';'))
                    {
                        if (!list.Contains(item))
                        {
                            list.Add(splitItem);
                        }
                    }
                    
                }
                foreach (var item in results.Select(s => s.Sellers.Trim()))
                {
                    foreach (string splitItem in item.Split(';'))
                    {
                        if (!list.Contains(item))
                        {
                            list.Add(splitItem);
                        }
                    }

                }
                var sellers = results.Select(s => s.Sellers.Trim().Split(';')).ToList().Distinct();
                list.AddRange(results.Select(s => s.Hydrocarbon).Distinct());
                list.AddRange(results.Select(s => s.OpNonOp).Distinct());
                list.AddRange(results.Select(s => s.Resource_Type).Distinct());
                list.AddRange(results.Select(s => s.US_Play).Distinct());
                list.AddRange(results.Select(s => s.US_Region).Distinct());
                foreach (var item in results.Select(s => s.Counties.Trim()))
                {
                    foreach (string splitItem in item.Split(';'))
                    {
                        if (!list.Contains(item))
                        {
                            list.Add(splitItem);
                        }
                    }

                }
                foreach (var item in results.Select(s => s.States.Trim()))
                {
                    foreach (string splitItem in item.Split(';'))
                    {
                        if (!list.Contains(item))
                        {
                            list.Add(splitItem);
                        }
                    }

                }
            }
            else if (map.WhichDb == "PORTFOLIOTAB")
            {
                List<PortfolioData> results = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(data);
             
                foreach (var item in results.Take(100).Select(s => s.Counties.Trim()))
                {
                    foreach (string splitItem in item.Split(';'))
                    {
                        if (!list.Contains(item))
                        {
                            list.Add(splitItem);
                        }
                    }

                }
               
             

            }
            else if (map.WhichDb == "DFSTAB")
            {
                List<DfsData> results = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(data);
                foreach (var item in results.Select(s => s.Sellers.Trim()))
                {
                    foreach (string splitItem in item.Split(';'))
                    {
                        if (!list.Contains(item))
                        {
                            list.Add(splitItem);
                        }
                    }

                }
                foreach (var item in results.Select(s => s.Agent_Broker.Trim()))
                {
                    foreach (string splitItem in item.Split(';'))
                    {
                        if (!list.Contains(item))
                        {
                            list.Add(splitItem);
                        }
                    }

                }
                var sellers = results.Select(s => s.Sellers.Trim().Split(';')).ToList().Distinct();
                list.AddRange(results.Select(s => s.DealType).Distinct());
                list.AddRange(results.Select(s => s.US_Play).Distinct());
                list.AddRange(results.Select(s => s.US_Region).Distinct());
               
            }

            return list;
        }

        protected List<IMapIndexData> GetFilteredData(List<T> data, MapSearch map)
        {
            List<string> theQueryString = new List<string>();
            List<string> andQueryString = new List<string>();
            List<string> companyQueryString = new List<string>();
            List<string> dealTypeQueryString = new List<string>();
            StringBuilder sb = new StringBuilder();
            List<IMapIndexData> retVal = new List<IMapIndexData>();
            theQueryString = map.WhichDb != "PORTFOLIOTAB" ? GetPlayFilters(theQueryString, map) : new List<string>();
            theQueryString = map.WhichDb != "PORTFOLIOTAB" ? GetRegionFilters(theQueryString, map) : new List<string>();
            andQueryString = GetDealTypeFilters(andQueryString, map);
            companyQueryString = GetCompanyFilters(companyQueryString, map);
            //andQueryString = GetRecordIdFilters(andQueryString, map);

            if (theQueryString.Count > 0 && sb.ToString() == "")
            {
                sb.Append("(").Append(string.Join(" OR ", theQueryString)).Append(")");

                //if (companyQueryString.Count > 0)
                //{
                //    sb.Append(" AND ").Append(string.Join(" OR ", companyQueryString));
                //}
                if (andQueryString.Count > 0)
                {
                    sb.Append(" AND (").Append(string.Join(" OR ", andQueryString)).Append(")");
                }
                if (dealTypeQueryString.Count > 0)
                {
                    sb.Append(" AND ").Append(string.Join(" OR ", dealTypeQueryString));
                }
            }

            else
            {
                if (companyQueryString.Count > 0 && andQueryString.Count > 0)
                {
                    sb.Append(string.Join(" OR ", companyQueryString)).Append(" AND (").Append(string.Join(" OR ", andQueryString)).Append(")");
                    if (dealTypeQueryString.Count > 0)
                    {
                        sb.Append(" AND ").Append(string.Join(" OR ", dealTypeQueryString));
                    }
                }
                else if (companyQueryString.Count > 0 && andQueryString.Count == 0)
                {
                    sb.Append(string.Join(" OR ", companyQueryString));
                    if (dealTypeQueryString.Count > 0)
                    {
                        sb.Append(" AND ").Append(string.Join(" OR ", dealTypeQueryString));
                    }
                }
                else if (companyQueryString.Count == 0 && andQueryString.Count > 0)
                {
                    sb.Append(string.Join(" OR ", andQueryString));
                }
                else if (true)
                {

                }
               
            }

            if (sb.ToString() != "")
            {
                if (map.WhichDb == "MATAB")
                {
                    retVal = QueryData(data, map, sb.ToString());
                }
                else if (map.WhichDb == "DFSTAB")
                {
                    retVal = QueryData(data, map, sb.ToString());
                }
                else if (map.WhichDb == "PORTFOLIOTAB")
                {
                    retVal = QueryData(data, map, sb.ToString());
                }
                else
                {
                    List<IMapIndexData> retValTimeRange = new List<IMapIndexData>();
                    foreach (T d in data)
                    {
                        retValTimeRange.Add((IMapIndexData)d);

                    }
                    retVal = TimeRange(sb.ToString(), retValTimeRange, map);
                    List<IMapIndexData> retValKeyword = new List<IMapIndexData>();
                    List<IMapIndexData> retValCompany = new List<IMapIndexData>();
                    if (!string.IsNullOrEmpty(map.TimeRange))
                    {
                        retValKeyword = retVal;
                        retValCompany = retVal;
                    }
                    else
                    {
                        foreach (T d in data)
                        {
                            retValKeyword.Add((IMapIndexData)d);
                            retValCompany.Add((IMapIndexData)d);

                        }
                    }
                    retVal = QueryData(data, map, sb.ToString());

                    if (map.WhichDb == "PORTFOLIOTAB")
                    {
                        List<IMapIndexData> retValPlay = retVal;
                        List<IMapIndexData> retValRegion = retVal;

                        retValPlay = PlaySearch(sb.ToString(), retValPlay, map);
                        retValRegion = RegionSearch(sb.ToString(), retValRegion, map);

                        if (map.Plays.Count > 0 && map.Regions.Count > 0)
                        {
                            retVal = retValPlay.Concat(retValRegion).ToList();
                        }
                        else if (map.Plays.Count > 0 && map.Regions.Count == 0)
                        {
                            retVal = retValPlay;
                        }
                        else if (map.Plays.Count == 0 && map.Regions.Count > 0)
                        {
                            retVal = retValRegion;
                        }
                    }

                    retValCompany = CompanySearch(sb.ToString(), retValCompany, map);
                    retValKeyword = KeywordSearch(sb.ToString(), retValKeyword, map);

                    if (!string.IsNullOrEmpty(map.Keyword))
                    {
                        if (map.Companies.Count > 0)
                        {
                            retVal = retValKeyword.Concat(retVal).Concat(retValCompany).ToList();
                        }
                        else
                        {
                            retVal = retValKeyword.Concat(retVal).ToList();
                        }
                    }
                    else if (map.Companies.Count > 0)
                    {
                        retVal = retVal.Concat(retValCompany).ToList();
                    }
                }
                

            }
            else if ((map.Plays.Count > 0 || map.Regions.Count > 0) && (string.IsNullOrEmpty(map.Keyword) || string.IsNullOrEmpty(map.TimeRange)) && map.WhichDb == "PORTFOLIOTAB")
            {
                List<IMapIndexData> vals = new List<IMapIndexData>(data.Count);

                foreach (T d in data)
                {
                    vals.Add((IMapIndexData)d);
                }

                List<IMapIndexData> retValPlay = vals;

                List<IMapIndexData> retValRegion = vals;

                retValPlay = PlaySearch(sb.ToString(), retValPlay, map);

                retValRegion = RegionSearch(sb.ToString(), retValRegion, map);

                if (map.Plays.Count > 0 && map.Regions.Count > 0)
                {
                    retVal = retValPlay.Concat(retValRegion).ToList();
                }

                else if (map.Plays.Count > 0 && map.Regions.Count == 0)
                {
                    retVal = retValPlay;
                }

                else if (map.Plays.Count == 0 && map.Regions.Count > 0)
                {
                    retVal = retValRegion;
                }
                if (!string.IsNullOrEmpty(map.Keyword))
                {
                    retVal = KeywordSearch(sb.ToString(), retVal.Count > 0 ? retVal : vals, map);
                }
                //retVal = PlaySearch(sb.ToString(), vals, map);
            }
            else if (!string.IsNullOrEmpty(map.Keyword) || !string.IsNullOrEmpty(map.TimeRange))
            {
                List<IMapIndexData> vals = new List<IMapIndexData>(data.Count);

                foreach (T d in data)
                {
                    vals.Add((IMapIndexData)d);
                }

                if (map.Plays.Count > 0)
                {
                    retVal = PlaySearch(sb.ToString(), vals, map);
                }
                if (!string.IsNullOrEmpty(map.TimeRange))
                {
                    retVal = TimeRange(sb.ToString(), vals, map);
                }
                if (!string.IsNullOrEmpty(map.Keyword))
                {
                    retVal = KeywordSearch(sb.ToString(), retVal.Count > 0 ? retVal : vals, map);
                }


            }
            else
            {
                List<IMapIndexData> vals = new List<IMapIndexData>(data.Count);

                foreach (T d in data)
                {
                    vals.Add((IMapIndexData)d);
                }
            
                retVal = TimeRange(sb.ToString(), vals, map);
            }

            return retVal;
        }

        internal virtual List<string> GetPlayFilters(List<string> theQueryString, MapSearch map)
        {
            try
            {
                if (map.Plays != null)
                {
                    if (map.Plays.Count > 0)
                    {
                        theQueryString.AddRange(BuildEqualQuery("PlayId", map.Plays.OfType<string>().ToList()));
                    }
                }
                
            }
            catch (Exception ex)
            {
                
            }
           
            return theQueryString;
        }
        internal virtual List<string> GetRegionFilters(List<string> theQueryString, MapSearch map)
        {
            try
            {
                if (map.Regions != null)
                {
                    if (map.Regions.Count > 0)
                    {
                        theQueryString.AddRange(BuildEqualQuery("RegionId", map.Regions));
                    }
                }
                
            }
            catch (Exception ex)
            {

            }
            
            return theQueryString;
        }
        internal virtual List<string> GetDealTypeFilters(List<string> theQueryString, MapSearch map)
        {
            try
            {
                if (map.Entities != null)
                {
                    if (map.Entities.Count > 0)
                    {
                        theQueryString.AddRange(BuildEqualQuery("DealTypeId", map.Entities));
                    }

                }
               
            }
            catch (Exception ex)
            {

            }
            
            return theQueryString;
        }
        internal virtual List<string> GetRecordIdFilters(List<string> theQueryString, MapSearch map)
        {
            return theQueryString;
        }

        internal virtual List<string> GetCompanyFilters(List<string> theQueryString, MapSearch map)
        {
            List<string> andQueryList = new List<string>();

            if (map.Companies != null)
            {
                if (map.Companies.Count > 0)
                {
                    try
                    {
                        if (theQueryString.Count > 0)
                        {
                            theQueryString.AddRange(BuildContainsQuery("BuyerIdsString", map.Companies));
                            theQueryString.AddRange(BuildContainsQuery("SellerIdsString", map.Companies));

                        }
                        else
                        {
                            theQueryString.AddRange(BuildContainsQuery("BuyerIdsString", map.Companies));
                            theQueryString.AddRange(BuildContainsQuery("SellerIdsString", map.Companies));
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
            
            return theQueryString;
        }
        
        internal abstract List<IMapIndexData> QueryData(List<T> data, MapSearch map, string query);
    }
}