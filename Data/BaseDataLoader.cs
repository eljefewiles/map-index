using MapIndex.Infrastructure.Caching;
using MapIndex.Models.MA;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Dapper;
using System.Configuration;
using MapIndex.Infrastructure.Logging;
using MapIndex.Models.Portfolio;
using MapIndex.Models;
using MapIndex.Models.ViewModels;
using System.Text;
using MapIndex.Models.DFS;

namespace MapIndex.Data
{
    public static class BaseDataLoader<T>
    {
        public static string cacheKeyMaData = "MADATACACHEKEY";
        public static string cacheKeyPortfolio = "CACHE_KEY_PORTFOLIODATA";

        private static ICacheProvider _cacheProvider = new Infrastructure.Caching.MemoryCacheProvider();

        public static IDbConnection GetConnection(string conStr)
        {
            return new SqlConnection(conStr);
        }

        public static string BaseMaDataQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"SELECT ma.theId, ma.id, ma.DealTypeId, ma.DealType, ma.Buyers, case when ma.buyerIds IS NULL THEN '' ELSE ma.buyerIds END AS BuyerIdsString, 
ma.Sellers, case when ma.sellerIds IS NULL THEN '' ELSE ma.sellerIds END AS SellerIdsString, 
ma.Date_Announced, ma.Value___MM_, ma.__Acre,
ma.__Daily_BOE,ma.Hydrocarbon, ma.Counties, ma.States, ma.US_Region, ma.US_Play, ma.Latitude AS Lat, 
ma.Longitude AS Long, 'MA' AS Icon, ma.Headline, 'bjquik34u' AS DBID, ma.OpNonOp, ma.Resource_Type,
PlayId, ma.regionid AS RegionId, case when ma.HexColor IS NULL THEN 'blue' ELSE ma.HexColor END AS HexColor,
case when ma.buyerIds IS NOT NULL then b.image ELSE '' END AS BuyerLogo,
case when ma.sellerIds IS NOT NULL then s.image ELSE '' END AS SellerLogo
FROM [MapIndex].[dbo].[maGeometryView] AS ma
LEFT OUTER JOIN PLSX_Production.dbo.tblCompany b ON b.id = case when CHARINDEX(';',ma.buyerIds,1) = 0 then ma.buyerIds ELSE SUBSTRING(ma.buyerIds, 0, charindex(';', ma.buyerIds, 0)) END
LEFT OUTER JOIN PLSX_Production.dbo.tblCompany s ON s.id = case when CHARINDEX(';',ma.sellerIds,1) = 0 then ma.sellerIds ELSE SUBSTRING(ma.sellerIds, 0, charindex(';', ma.sellerIds, 0)) END 
WHERE ma.Date_Announced >= '01/01/2014' AND ma.[LiveDeal] = 1 AND ma.Latitude IS NOT NULL AND ma.Longitude IS NOT NULL
ORDER BY ma.Date_Announced DESC");
            return sb.ToString();
        }

        public static List<Models.SourceDoc> GetSourceMapDoc()
        {
            List<Models.SourceDoc> maDocs = new List<Models.SourceDoc>();

            try
            {
                using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
                {
                    con.Open();

                    maDocs = con.Query<Models.SourceDoc>(@"SELECT US_Deal_ID__ref_ AS Id, 
                                                [File] AS SourceDocument, Quick_Comment AS QuickComment, Document_Type AS DocumentType 
                                        FROM MapIndex.dbo.[bjquik4qj]
                                        WHERE US_Deal_ID__ref_ IS NOT NULL AND Document_Type = '" + "ShapeFile" + "' OR Document_Type = '" + "Map" + "'").ToList();

                }
            }
            catch (Exception ex)
            {
                LogFactory.Instance.Log().WriteMessage("Error Getting Map Queries: " + ex.Message);
            }

            return maDocs;
        }

        public static string PortfolioCompanyQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"SELECT companyId, companyName, HexColor FROM (
SELECT ROW_NUMBER() OVER(PARTITION BY c.companyId ORDER BY c.companyId ASC) rn, c.companyId AS companyId, c.companyName AS companyName, 
'red' AS HexColor FROM [MapIndex].[synch].[company] AS c
INNER JOIN MapIndex.geo.portfolioGeometry g ON g.companyId = c.companyId 
WHERE c.companyName IS NOT NULL AND g.PlayIds IS NOT NULL 
)
t
WHERE rn = 1
ORDER BY companyName ASC");
            return sb.ToString();
        }

        public static string CompanyQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"SELECT companyId, companyName FROM (SELECT ROW_NUMBER() OVER(PARTITION BY companyId ORDER BY companyId ASC) AS rn, 
                companyId, companyName FROM [MapIndex].[synch].[company] WHERE companyName <> '' AND companyName IS NOT NULL) t WHERE rn = 1 ORDER BY companyName ASC");

            return sb.ToString();
        }

        public static string RegionQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ").AppendLine();
            sb.Append(" regionid ").Append(" AS ").Append(" regionid ").Append(", ").AppendLine();
            sb.Append(" RegionName ").Append(" AS ").Append(" RegionName ").AppendLine();
            sb.Append(" FROM [MapIndex].[dbo].[RegionGeometry] ORDER BY RegionName ASC").AppendLine();

            return sb.ToString();
        }

        public static string PlayQuery()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ").AppendLine();
            sb.Append(" id ").Append(" AS ").Append(" id ").Append(", ").AppendLine();
            sb.Append(" PlayName ").Append(" AS ").Append(" PlayName ").AppendLine();
            sb.Append(" FROM [MapIndex].[dbo].[PlayGeometry] ORDER BY PlayName ASC").AppendLine();

            return sb.ToString();
        }

        public static string PortfolioBaseDataQuery()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                string query = @"SELECT * FROM 
 ( 
 SELECT ROW_NUMBER() OVER(PARTITION BY c.companyId ORDER BY c.companyId ASC) AS rn,
 c.companyName AS CompanyName,
 case when c.companyId IS NULL THEN '' ELSE c.companyId END  AS CompanyId,
 case when p.play IS NULL THEN '' ELSE p.play END  AS  US_Play,
 case when p.county IS NULL THEN '' ELSE p.county END  AS  Counties,
 case when p.region IS NULL THEN '' ELSE p.region END  AS  US_Region,
 case when p.state IS NULL THEN '' ELSE p.state END  AS  States,
 case when p.PlayIds IS NULL THEN '' ELSE p.PlayIds END  AS  PlayId,
 case when p.RegionIds IS NULL THEN '' ELSE p.RegionIds END  AS  RegionId,
 case when p.HexColor IS NULL THEN 'red' ELSE p.HexColor END  AS  HexColor,
 p.[theGeom].STCentroid().STX AS Longitude,
 p.[theGeom].STCentroid().STY AS Latitude,
 cl.address AS Address, cl.city AS City, cl.state AS State, cl.zip AS Zip, cl.phone AS Phone,
 case when co.image IS NULL then '' ELSE  co.image  END AS Logo
 FROM MapIndex.geo.portfolioGeometry  AS  p 
 INNER JOIN MapIndex.[synch].[company]  c  ON c.companyId = p.companyId
 INNER JOIN PLSX_Production.dbo.tblCompany co ON co.id = c.companyId
 INNER JOIN PLSX_Production.dbo.tblCompanyLocations cl ON cl.cid = co.id
 WHERE c.companyName  IS NOT NULL 
 )
 t 
 WHERE rn = 1
ORDER BY CompanyName ASC";
                //sb.Append("SELECT * FROM ").AppendLine();
                //sb.Append(" ( ").AppendLine();
                //sb.Append(" SELECT ROW_NUMBER() OVER(PARTITION BY c.companyId ORDER BY c.companyId ASC) AS rn").Append(",").AppendLine();
                //sb.Append(" c.companyName").Append(" AS ").Append("CompanyName").Append(",").AppendLine();
                //sb.Append(" case when c.companyId IS NULL THEN ''").Append(" ELSE ").Append("c.companyId").Append(" END ").Append(" AS ").Append("CompanyId").Append(",").AppendLine();
                //sb.Append(" case when p.play IS NULL THEN ''").Append(" ELSE ").Append("p.play").Append(" END ").Append(" AS ").Append(" US_Play").Append(",").AppendLine();
                //sb.Append(" case when p.county IS NULL THEN ''").Append(" ELSE ").Append("p.county").Append(" END ").Append(" AS ").Append(" Counties").Append(",").AppendLine();
                //sb.Append(" case when p.region IS NULL THEN ''").Append(" ELSE ").Append("p.region").Append(" END ").Append(" AS ").Append(" US_Region").Append(",").AppendLine();
                //sb.Append(" case when p.state IS NULL THEN ''").Append(" ELSE ").Append("p.state").Append(" END ").Append(" AS ").Append(" States").Append(",").AppendLine();
                //sb.Append(" case when p.PlayIds IS NULL THEN ''").Append(" ELSE ").Append("p.PlayIds").Append(" END ").Append(" AS ").Append(" PlayId").Append(",").AppendLine();
                //sb.Append(" case when p.RegionIds IS NULL THEN ''").Append(" ELSE ").Append("p.RegionIds").Append(" END ").Append(" AS ").Append(" RegionId").Append(",").AppendLine();
                //sb.Append(" case when p.HexColor IS NULL THEN 'red'").Append(" ELSE ").Append("p.HexColor").Append(" END ").Append(" AS ").Append(" HexColor").Append(",").AppendLine();
                //sb.Append(" p.[theGeom]").Append(".STCentroid().STX").Append(" AS ").Append("Longitude").Append(",").AppendLine();
                //sb.Append(" p.[theGeom]").Append(".STCentroid().STY").Append(" AS ").Append("Latitude").AppendLine();
                //sb.Append(" FROM MapIndex.geo.portfolioGeometry ").Append(" AS ").Append(" p ").AppendLine();
                //sb.Append(" INNER JOIN MapIndex.[synch].[company] ").Append(" c ").Append(" ON ").Append("c.").Append("companyId").Append(" = ").Append("p.").Append("companyId").AppendLine();
                //sb.Append(" WHERE ").Append("c.").Append("companyName ").Append(" IS NOT NULL ").AppendLine();
                //sb.Append(" )").AppendLine();
                //sb.Append(" t ").AppendLine();
                //sb.Append(" WHERE ").Append("rn").Append(" = ").Append("1").AppendLine();
                sb.Append(query);
            }
            catch (Exception ex)
            {
            }

            return sb.ToString();
        }

        public static string MaBaseDataQuery()
        {
            string query = @"SELECT theId, id, CASE WHEN DealTypeId IS NULL THEN '' ELSE DealTypeId END AS DealTypeId, CASE WHEN DealType IS NULL THEN '' ELSE DealType END AS DealType, CASE WHEN Buyers IS NULL THEN '' ELSE Buyers END AS Buyers, case when buyerIds IS NULL THEN '' ELSE buyerIds END AS BuyerIdsString, CASE WHEN Sellers IS NULL THEN '' ELSE Sellers END AS Sellers, 
                                    case when sellerIds IS NULL THEN '' ELSE sellerIds END AS SellerIdsString, Date_Announced, CASE WHEN Value___MM_ IS NULL THEN '' ELSE Value___MM_ END AS Value___MM_, CASE WHEN __Acre IS NULL THEN '' ELSE __Acre END AS __Acre,
                                    case when __Daily_BOE IS NULL THEN '' ELSE __Daily_BOE END AS __Daily_BOE,CASE WHEN Hydrocarbon IS NULL THEN '' ELSE Hydrocarbon END AS Hydrocarbon, Counties, CASE WHEN States IS NULL THEN '' ELSE States END AS States, CASE WHEN US_Region IS NULL THEN '' ELSE US_Region END AS US_Region, CASE WHEN US_Play IS NULL THEN '' ELSE US_Play END AS US_Play, CASE WHEN Latitude IS NULL THEN Latitude  END AS Lat, 
                                    case when Longitude IS NULL THEN '' ELSE Longitude END AS Long, 'MA' AS Icon, case when Headline IS NULL THEN '' ELSE Headline END AS Headline, 'bjquik34u' AS DBID, OpNonOp, case when Resource_Type IS NULL THEN '' ELSE Resource_Type END AS Resource_Type,
                                    case when PlayId IS NULL THEN '' ELSE PlayId END AS PlayId, case when regionid IS NULL THEN '' ELSE regionid END AS RegionId, case when HexColor IS NULL THEN 'blue' ELSE HexColor END AS HexColor
                                    FROM [MapIndex].[dbo].[maGeometryView]
                                    WHERE Date_Announced >= '01/01/2014' AND [LiveDeal] = 1 AND Latitude IS NOT NULL AND Longitude IS NOT NULL
                                    ORDER BY Date_Announced DESC
                                    ";

            return query;
        }

        public static string DfsBaseDataQuery()
        {
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

            return query;
        }

        internal static List<T> GetProperList(List<IMapIndexData> data)
        {
            List<T> vals = new List<T>(data.Count);

            foreach (IMapIndexData d in data)
            {
                vals.Add((T)d);
            }
            return vals;
        }

        internal static List<IMapIndexData> GetNonProperList(List<T> data)
        {
            List<IMapIndexData> vals = new List<IMapIndexData>(data.Count);

            foreach (T d in data)
            {
                vals.Add((IMapIndexData)d);
            }
            return vals;
        }

        internal static List<Models.SourceDoc> GetSourceMapDocDfs()
        {
            List<Models.SourceDoc> sourceDocs = new List<Models.SourceDoc>();

            using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
            {
                con.Open();
                string query = @"SELECT listing_id AS Id, [File] AS SourceDocument, Quick_Comment AS QuickComment, Document_Type AS DocumentType FROM MapIndex.dbo.[bjquik4qj]
                                        WHERE listing_id IS NOT NULL AND (Document_Type = '" + "ShapeFile" + "' OR Document_Type = '" + "Map" + "')";
                sourceDocs = con.Query<Models.SourceDoc>(query).ToList();
            }

            return sourceDocs;
        }

        internal static List<IMapIndexData> GetData(string query, string cacheKey)
        {
            List<IMapIndexData> retVal = new List<IMapIndexData>();

            try
            {
                using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
                {
                    con.Open();
                    List<MaData> maDeals = new List<MaData>();
                    List<DfsData> dfsDeals = new List<DfsData>();
                    List<PortfolioData> portData = new List<PortfolioData>();

                    if (cacheKey.ToLower().IndexOf("ma") != -1)
                    {
                        List<Models.SourceDoc> docs = GetSourceMapDoc();
                        maDeals = con.Query<MaData>(query).ToList();
                        foreach (var item in maDeals)
                        {
                            try
                            {
                                item.SourceDocs = docs.Where(s => s.Id == item.id).ToList();
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        foreach (var item in maDeals)
                        {
                            try
                            {
                                item.BuyerIds = item.BuyerIdsString.Trim().Split(';').ToList();

                            }
                            catch (Exception ex)
                            {

                            }

                            try
                            {
                                item.SellerIds = item.SellerIdsString.Trim().Split(';').ToList();

                            }
                            catch (Exception ex)
                            {

                            }

                        }
                        foreach (MaData d in maDeals)
                        {
                            retVal.Add((IMapIndexData)d);
                        }
                    }
                    else if (cacheKey.ToLower().IndexOf("dfs") != -1)
                    {
                        List<Models.SourceDoc> docs = GetSourceMapDocDfs();
                        dfsDeals = con.Query<DfsData>(query).ToList();
                        foreach (var item in dfsDeals)
                        {
                            item.SourceDocs = docs.Where(s => s.Id == item.id).ToList();
                        }
                        foreach (DfsData d in dfsDeals)
                        {
                            retVal.Add((IMapIndexData)d);
                        }

                    }
                    else if (cacheKey.ToLower().IndexOf("portfolio") != -1)
                    {
                        portData = con.Query<PortfolioData>(query).ToList();


                        foreach (PortfolioData d in portData)
                        {
                            retVal.Add((IMapIndexData)d);
                        }


                    }
                  
                    if (retVal.Count > 0)
                    {
                        _cacheProvider.Store(cacheKey, retVal, int.MaxValue);

                    }
                }
            }
            catch (Exception ex)
            {
                LogFactory.Instance.Log().WriteMessage("Error getting Ma Data: " + ex.Message, ex);
            }

            return retVal;
        }

        public static DataTable GetDataTable(string query, string cacheKey)
        {
            DataTable dt = new DataTable();

            try
            {
                using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
                {
                    con.Open();
                    dt.Load(con.ExecuteReader(query));
                }

                if (dt.Rows.Count > 0)
                {
                    _cacheProvider.Store(cacheKey, dt, int.MaxValue);
                }
            }
            catch (Exception ex)
            {
                LogFactory.Instance.Log().WriteMessage("", ex);
            }

            return dt;
        }

        public static List<Company> BasePortfolioCompanies(DataTable dt, List<IMapIndexData> data, string cacheKey)
        {
            List<Company> companies = new List<Company>();
            List<PortfolioData> baseData = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(data);
            foreach (System.Data.DataRow dr in dt.Rows)
            {
                Company company = new Company();

                try
                {
                    company.DealCount = baseData.Where(s => s.CompanyId.ToString() != null).Where(s => s.CompanyId.ToString() == dr["companyId"].ToString()).Count();

                }
                catch (Exception e)
                {

                }
                company.CompanyId = dr["companyId"].ToString();
                company.CompanyName = dr["companyName"].ToString();
                company.Id = Convert.ToInt32(dr["companyId"]);
                company.HexColor = baseData.Where(s => s.CompanyId == dr["companyId"].ToString()).Select(s => s.HexColor).FirstOrDefault();

                if (company.DealCount > 0)
                {
                    companies.Add(company);
                }              
            }
            if (companies.Any())
            {
                _cacheProvider.Store(cacheKey, companies, int.MaxValue);
            }
            return companies.OrderBy(s => s.CompanyName).ToList();
        }

//        private static List<Play> GetPlaysPortfolio(List<IMapIndexData> data, MapSearch map)
//        {
//            StringBuilder cacheKey = new StringBuilder();
//            cacheKey.Append("CACHE_KEY_PORTFOLIO_PLAYS");

//            List<Play> plays = new List<Play>();

//            if (!plays.Any() || plays == null)
//            {

//                string query = @"SELECT id, PlayName FROM [MapIndex].[dbo].[PlayGeometry]";

//                System.Data.DataTable dt = GetDataTable(query);

//                List<PortfolioData> vals = new List<PortfolioData>(data.Count);

//                foreach (IMapIndexData d in data)
//                {
//                    vals.Add((PortfolioData)d);
//                }




//                foreach (System.Data.DataRow dr in dt.Rows)
//                {
//                    Play play = new Play();

//                    /*
//                    play.DealCount = data.Where(s => s.PlayId == dr["id"].ToString()).Count();
//                    */

//                    try
//                    {
//                        play.DealCount = vals.Where(s => s.US_Play.Contains(dr["PlayName"].ToString())).Count();
//                        //play.DealCount = vals.Where(s => s.PlayId.Contains(dr["id"].ToString())).Count();


//                    }
//                    catch (Exception ex)
//                    {

//                    }

//                    play.PlayName = dr["PlayName"].ToString();

//                    play.IsSelected = false;
//                    play.Id = Convert.ToInt32(dr["id"]);

//                    plays.Add(play);
//                }

//                plays = plays.OrderBy(s => s.PlayName).ToList();

//                if (plays.Any())
//                {
//                    _cacheProvider.Store(cacheKey.ToString(), plays, int.MaxValue);
//                }
//            }


//            return plays;

//        }

//        private static List<Company> GetCompaniesPortfolio(List<IMapIndexData> data, MapSearch map)
//        {
//            StringBuilder cacheKey = new StringBuilder();

//            cacheKey.Append("CACHE_KEY_PORTFOLIO_COMPANIES");
//            cacheKey.Append("_").Append("RELOADCOMPANIES").Append("False");

//            List<Company> companies = new List<Company>();
//            if (!companies.Any() || companies == null)
//            {

//                string query = @"SELECT companyId, companyName, HexColor FROM (
//SELECT ROW_NUMBER() OVER(PARTITION BY c.companyId ORDER BY c.companyId ASC) rn, c.companyId AS companyId, c.companyName AS companyName, 
//'red' AS HexColor FROM [MapIndex].[synch].[company] AS c
//INNER JOIN MapIndex.geo.portfolioGeometry g ON g.companyId = c.companyId 
//WHERE c.companyName IS NOT NULL AND g.PlayIds IS NOT NULL 
//)
//t
//WHERE rn = 1
//ORDER BY companyName ASC";

//                System.Data.DataTable dt = GetDataTable(query);


//                List<PortfolioData> vals = new List<PortfolioData>(data.Count);

//                foreach (IMapIndexData d in data)
//                {
//                    vals.Add((PortfolioData)d);
//                }
//                foreach (System.Data.DataRow dr in dt.Rows)
//                {
//                    Company company = new Company();

//                    try
//                    {
//                        company.DealCount = vals.Where(s => s.CompanyId.ToString() != null).Where(s => s.CompanyId.ToString() == dr["companyId"].ToString()).Count();

//                    }
//                    catch (Exception e)
//                    {

//                    }
//                    company.CompanyId = dr["companyId"].ToString();
//                    company.CompanyName = dr["companyName"].ToString();
//                    company.Id = Convert.ToInt32(dr["companyId"].ToString());

//                    company.HexColor = vals.Where(s => s.CompanyId == dr["companyId"].ToString()).Select(s => s.HexColor).FirstOrDefault();

//                    if (map.RefreshCompany == "true")
//                    {
//                        if (map.ReloadCompanies == false)
//                        {
//                            if (company.DealCount > 0)
//                            {
//                                companies.Add(company);
//                            }
//                        }
//                        else
//                        {
//                            companies.Add(company);
//                        }
//                    }
//                    else
//                    {
//                        if (company.DealCount > 0)
//                        {
//                            companies.Add(company);
//                        }
//                    }

//                }
//                companies = companies.OrderBy(s => s.CompanyName).ToList();
//                if (companies.Any())
//                {
//                    _cacheProvider.Store(cacheKey.ToString(), companies, int.MaxValue);
//                }
//            }


//            return companies;

      

//        }

//        private static List<Region> GetRegionsPortfolio(List<IMapIndexData> data, MapSearch map)
//        {
//            StringBuilder cacheKey = new StringBuilder();
//            cacheKey.Append("CACHE_KEY_PORTFOLIO_REGIONS");


//            List<Region> regions = new List<Region>();
//            if (!regions.Any() || regions == null)
//            {
//                string query = @"SELECT regionid, RegionName FROM [MapIndex].[dbo].[RegionGeometry]";

//                System.Data.DataTable dt = GetDataTable(query);
//                List<PortfolioData> vals = new List<PortfolioData>(data.Count);

//                foreach (IMapIndexData d in data)
//                {
//                    vals.Add((PortfolioData)d);
//                }

//                foreach (System.Data.DataRow dr in dt.Rows)
//                {
//                    Region region = new Region();
//                    region.RegionName = dr["RegionName"].ToString();

//                    if (region.RegionName == "Permian")
//                    {
//                        // region.DealCount = ma.Where(s => s.RegionId == "13" || s.RegionId == "11" || s.RegionId == "7").Count();
//                    }
//                    else
//                    {
//                        // region.DealCount = ma.Where(s => s.RegionId == dr["regionid"].ToString()).Count();
//                    }

//                    try
//                    {
//                        //region.LayerGroupName = MaPlayLayerGroups()[dr["RegionName"].ToString()];
//                        region.DealCount = vals.Where(s => s.US_Region.Contains(dr["RegionName"].ToString())).Count();
//                    }
//                    catch (Exception ex)
//                    {
//                        LogFactory.Instance.Log().WriteMessage("Error Getting Map Queries: " + ex.Message);
//                    }

//                    region.IsSelected = false;
//                    region.Id = Convert.ToInt32(dr["regionid"]);
//                    regions.Add(region);
//                }
//                if (regions.Any())
//                {
//                    _cacheProvider.Store(cacheKey.ToString(), regions, int.MaxValue);
//                }
//            }
                

//            return regions;
//        }

//        public static SideFilterOptions GetBaseSideFiltersPortfolio(List<IMapIndexData> data, MapSearch map)
//        {
//            SideFilterOptions sideFilter = new SideFilterOptions();

//            List<PortfolioData> vals = new List<PortfolioData>();

//            sideFilter.Plays = GetPlaysPortfolio(data, map);

//            // BRING ORIGINAL COMPANY LIST IF PLAYS OR ANY OTHER SIDE FILTERS ARE SELECTED
//            sideFilter.Companies = GetCompaniesPortfolio(data, map);

//            sideFilter.Regions = GetRegionsPortfolio(data, map);

//            return sideFilter;
//        }

//        public static List<IMapIndexData> GetPortfolioData()
//        {
//            string cacheKey = cacheKeyPortfolio;
//            List<IMapIndexData> items = new List<IMapIndexData>();

//            if (!items.Any() || items == null)
//            {
//                try
//                {
//                    using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
//                    {
//                        con.Open();

//                        string query = @"SELECT * FROM (SELECT ROW_NUMBER() OVER(PARTITION BY c.companyId ORDER BY c.companyId ASC) AS rn, c.companyName AS CompanyName, case when c.companyId IS NULL THEN '' ELSE c.companyId END AS CompanyId, 
//                                        case when p.play IS NULL THEN '' ELSE p.play END AS US_Play, 
//                                        case when p.county IS NOT NULL THEN p.county ELSE '' END AS Counties, 
//                                        case when p.region IS NOT NULL THEN p.region ELSE '' END AS US_Region, 
//                                        case when p.state IS NULL THEN '' ELSE p.state END AS States, case when p.PlayIds IS NULL THEN '' ELSE p.PlayIds END AS PlayId, 
//                                        case when p.RegionIds IS NULL THEN '' ELSE p.RegionIds END AS RegionId,
//                                        case when HexColor IS NOT NULL then HexColor ELSE 'red' END AS HexColor,
//                                        [theGeom].STCentroid().STX AS Longitude,
//	                                    [theGeom].STCentroid().STY AS Latitude
//                                        FROM MapIndex.geo.portfolioGeometry AS p
//                                        INNER JOIN MapIndex.[synch].[company] c ON c.companyId = p.companyId
//                                        WHERE c.companyName IS NOT NULL
//										)
//										t
//										WHERE rn = 1";
//                        //retVal.Add(new PortfolioData { CompanyId = 1 });
//                        //retVal = con.Query<IMapIndexData>(query).ToList();

//                        items = con.Query<PortfolioData>(query).ToList<IMapIndexData>();

//                    }

//                    if (items.Any())
//                    {
//                        _cacheProvider.Store(cacheKey, items, int.MaxValue);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    LogFactory.Instance.Log().WriteMessage("Error getting Ma Data: " + ex.Message, ex);
//                }

//            }

//            return items;

           
//        }

//        public static List<IMapIndexData> GetMaData()
//        {
//            string cacheKey = cacheKeyMaData;

//            List<IMapIndexData> items = new List<IMapIndexData>();

//            if (!items.Any() || items == null)
//            {
//                //List<MaData> retVal = new List<MaData>();

//                List<Models.SourceDoc> maDocs = GetSourceMapDoc();

//                try
//                {
//                    using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
//                    {
//                        con.Open();

//                        string query = @"SELECT theId, id, DealTypeId, DealType, Buyers, case when buyerIds IS NULL THEN '' ELSE buyerIds END AS BuyerIdsString, Sellers, case when sellerIds IS NULL THEN '' ELSE sellerIds END AS SellerIdsString, Date_Announced, Value___MM_, __Acre,
//                                    __Daily_BOE,Hydrocarbon, Counties, States, US_Region, US_Play, Latitude AS Lat, 
//                                    Longitude AS Long, 'MA' AS Icon, Headline, 'bjquik34u' AS DBID, OpNonOp, Resource_Type,
//                                    PlayId, regionid AS RegionId, HexColor
//                                    FROM [MapIndex].[dbo].[maGeometryView]
//                                    WHERE Date_Announced >= '01/01/2014' AND [LiveDeal] = 1 AND Latitude IS NOT NULL AND Longitude IS NOT NULL
//                                    ORDER BY Date_Announced DESC
//                                    ";

//                        items = con.Query<MaData>(query).ToList<IMapIndexData>();

//                        List<MaData> vals = new List<MaData>(items.Count);

//                        foreach (IMapIndexData d in items)
//                        {
//                            vals.Add((MaData)d);
//                        }

//                        foreach (var item in vals)
//                        {
//                            try
//                            {
//                                item.SourceDocs = maDocs.Where(s => s.Id == item.id).ToList();
//                            }
//                            catch (Exception ex)
//                            {
//                            }
//                        }
//                        foreach (var item in vals)
//                        {
//                            try
//                            {
//                                item.BuyerIds = item.BuyerIdsString.Trim().Split(';').ToList();

//                            }
//                            catch (Exception ex)
//                            {

//                            }

//                            try
//                            {
//                                item.SellerIds = item.SellerIdsString.Trim().Split(';').ToList();

//                            }
//                            catch (Exception ex)
//                            {

//                            }

//                        }
//                    }

//                    if (items.Any())
//                    {
//                        _cacheProvider.Store(cacheKey, items, int.MaxValue);
//                    }

//                }
//                catch (Exception ex)
//                {
//                    LogFactory.Instance.Log().WriteMessage("Error getting Ma Data: " + ex.Message, ex);
//                }

//            }
//            return items;

//        }

//        public static List<Models.SourceDoc> GetDfsSourceMapDoc()
//        {
//            List<Models.SourceDoc> sourceDocs = new List<Models.SourceDoc>();

//            using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
//            {
//                con.Open();
//                string query = @"SELECT listing_id AS Id, [File] AS SourceDocument, Quick_Comment AS QuickComment, Document_Type AS DocumentType FROM MapIndex.dbo.[bjquik4qj]
//                                        WHERE listing_id IS NOT NULL AND (Document_Type = '" + "ShapeFile" + "' OR Document_Type = '" + "Map" + "')";
//                sourceDocs = con.Query<Models.SourceDoc>(query).ToList();
//            }

//            return sourceDocs;
//        }

//        public static List<IMapIndexData> GetDfsData()
//        {
//            string cacheKey = "DFSDATACACHEKEY";

//            List<IMapIndexData> items = new List<IMapIndexData>();

//            if (!items.Any() || items == null)
//            {
//                //List<MaData> retVal = new List<MaData>();


//                List<Models.SourceDoc> sourceDocs = GetDfsSourceMapDoc();
//                string query = @"SELECT id, theId, Headline, case when AgentId IS NULL THEN '' ELSE AgentId END AS AgentId, case when SellerId IS NULL THEN '' ELSE SellerId END AS SellerId, case when Seller IS NULL THEN '' ELSE Seller END AS Sellers, case when Agent_Broker IS NULL THEN '' else Agent_Broker END AS Agent_Broker, ListingCode AS PLS_Code, RegionName AS US_Region, RegionId,
//                             PlayName AS US_Play, PlayId, DBID, Icon, DealType, DealTypeId, Lat, Long, Listing_Date, case when HexColor IS NULL THEN 'blue' ELSE HexColor END AS HexColor 
//                             FROM [MapIndex].[dbo].[listingGeometryView]";

//                using (var con = GetConnection(ConfigurationManager.ConnectionStrings["PLSX"].ConnectionString))
//                {
//                    con.Open();
//                    items = con.Query<DfsData>(query).ToList<IMapIndexData>();
//                }
//                List<DfsData> vals = new List<DfsData>(items.Count);

//                foreach (IMapIndexData d in items)
//                {
//                    vals.Add((DfsData)d);
//                }

//                foreach (var item in vals)
//                {
//                    item.SourceDocs = sourceDocs.Where(s => s.Id == item.id).ToList();
//                }

//                if (items.Any())
//                {
//                    _cacheProvider.Store(cacheKey, items, int.MaxValue);
//                }

//            }
//            return items;

//        }



    }
}