using MapIndex.Infrastructure.Configuration;
using MapIndex.Infrastructure.Logging;
using MapIndex.Models.MA;
using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Ionic.Zip;
using Newtonsoft.Json;
using MapIndex.Models.DFS;
using MapIndex.Data;
using System.Reflection;
using MapIndex.Models;
using MapIndex.Models.Portfolio;
using System.Net.Http.Headers;

namespace MapIndex.Controllers
{

    public class GeoserverController : ApiController
    {
        private Services.MaMapIndexService iService = new Services.MaMapIndexService(new Data.MA.MACacheRepository(new Data.MA.MARepository(),
                  new Infrastructure.Caching.MemoryCacheProvider()));
        private Services.DfsMapIndexService iDfsService = new Services.DfsMapIndexService(new Data.DFS.DFSCacheRepository(new Data.DFS.DFSRepository(),
new Infrastructure.Caching.MemoryCacheProvider()));
        //private Quickbase_API.Quickbase qApi = new Quickbase_API.Quickbase();

        private Services.GeoserverService iMaGeoService = new Services.GeoserverService(new Data.Geoserver.MA.MaGeoserverCacheRepository(new Data.Geoserver.MA.MaGeoserverRepository(new Infrastructure.EncodingHelpers.EncodingHelpers(), new Quickbase_API.Quickbase()),
            new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.DfsGeoserverService iDfsGeoService = new Services.DfsGeoserverService(new Data.Geoserver.DFS.DfsGeoserverCacheRepository(new Data.Geoserver.DFS.DfsGeoserverRepository(new Infrastructure.EncodingHelpers.EncodingHelpers(), new Quickbase_API.Quickbase()),
      new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.PortfolioGeoserverService iPortGeoService = new Services.PortfolioGeoserverService(new Data.Geoserver.Portfolio.PortfolioGeoserverCacheRepository(new Data.Geoserver.Portfolio.PortfolioGeoserverRepository(new Infrastructure.EncodingHelpers.EncodingHelpers(), new Quickbase_API.Quickbase()),
    new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.PortfolioDataService iPortfolioService = new Services.PortfolioDataService(new Data.Portfolio.PortfolioCacheRepository(new Data.Portfolio.PortfolioRepository(),
                new Infrastructure.Caching.MemoryCacheProvider()));

        private Services.MapIndexGeoserverService<IMapIndexData> iMaDataGeoService = new Services.MapIndexGeoserverService<IMapIndexData>(
            new GeoserverHelpers.MapIndex.MapIndexGeoserverCacheHelper<IMapIndexData>(new GeoserverHelpers.MapIndex.MapIndexGeoserverHelper<IMapIndexData>(),
              new Infrastructure.Caching.MemoryCacheProvider()));
        //private Infrastructure.EncodingHelpers.EncodingHelpers enc = new Infrastructure.EncodingHelpers.EncodingHelpers();


        private Services.AuthenticationService iAuth =
       new Services.AuthenticationService(new Data.Authentication.IAuthCacheRepository(new Data.Authentication.AuthRepository(),
           new Infrastructure.Caching.MemoryCacheProvider()));

        private Infrastructure.EncodingHelpers.EncodingHelpers enc = new Infrastructure.EncodingHelpers.EncodingHelpers();


        private Dictionary<string, string> GetLayerObject()
        {
            Dictionary<string, string> layers = new Dictionary<string, string>();

            //layers.Add("MA-DS:listingGeometryView", "DFS");
            //layers.Add("MA-DS:maGeometryView", "MA");
            //layers.Add("MA-DS:mapindex_companypositionscolors", "Portfolio");

            layers.Add(ConfigurationFactory.Instance.Configuration().GeoserverWmsDfsLayer, "DFS");
            layers.Add(ConfigurationFactory.Instance.Configuration().GeoserverWmsMaLayer, "MA");
            layers.Add(ConfigurationFactory.Instance.Configuration().GeoserverWmsPortfolioLayer, "Portfolio");
            layers.Add(ConfigurationFactory.Instance.Configuration().GeoserverWmsPortfolioInitialLayer, "PortfolioInitial");

            return layers;
        }

        private HtmlAgilityPack.HtmlDocument GetLayerObjectFromPointClick(GeoLayerParams datObject)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            string layers = null;
            StringBuilder sb = new StringBuilder();

            var request = new GeoLayerNoCql();

          
          

            try
            {
                using (var client = new System.Net.WebClient())
                {

                    //var deserializedGeoParamObject = Newtonsoft.Json.JsonConvert.DeserializeObject<GeoLayerParams>(datObject);
                    sb.AppendLine("Deserialized Geo Param Object: " + datObject).AppendLine();

                    client.Headers["X-Credentials"] = "private-user=" + ConfigurationFactory.Instance.Configuration().GeoserverWebuser
                        + "&private-pw=" + ConfigurationFactory.Instance.Configuration().GeoserverWebuserPassword;

                    sb.AppendLine("Headers").AppendLine();

                    sb.AppendLine().AppendLine("URL: " + ConfigurationFactory.Instance.Configuration().GeoServerBaseUrl + "/geoserver/gwc/service/wms?"
                        + datObject.ToString() + "&authKey=" + ConfigurationFactory.Instance.Configuration().GeoserverAuthKey).AppendLine();

                    if (datObject.cql_filter == null || datObject.cql_filter == "")
                    {
                        datObject.cql_filter = null;
                        request = JsonConvert.DeserializeObject<GeoLayerNoCql>(JsonConvert.SerializeObject(datObject,
                                     Newtonsoft.Json.Formatting.None,
                                     new JsonSerializerSettings
                                     {
                                         NullValueHandling = NullValueHandling.Ignore
                                     }));
                        layers = System.Web.HttpUtility.HtmlDecode(client.DownloadString(ConfigurationFactory.Instance.Configuration().GeoServerBaseUrl
                     + "/wms?" + request.ToString() + "&authKey=" + ConfigurationFactory.Instance.Configuration().GeoserverAuthKey));
                    }
                    else
                    {
                        layers = System.Web.HttpUtility.HtmlDecode(client.DownloadString(ConfigurationFactory.Instance.Configuration().GeoServerBaseUrl
                        + "/wms?" + datObject.ToString() + "&authKey=" + ConfigurationFactory.Instance.Configuration().GeoserverAuthKey));

                    }


                    doc.LoadHtml(layers);
                }
            }
            catch (Exception ex)
            {
                //Infrastructure.Logging.LogFactory.Instance.Log().GenerateEmail("Error Getting GetLayerObjectFromPointClick", ex.Message);

            }

            return doc;
        }

        private Dictionary<string, int> GetCellIdFromLayerObject()
        {
            Dictionary<string, int> cellId = new Dictionary<string, int>();

            //cellId.Add("MA", 2);
            //cellId.Add("DFS", 2);
            //cellId.Add("Portfolio", 4);

            cellId.Add("MA", ConfigurationFactory.Instance.Configuration().GeoserverWfsDealIdCellMA);
            cellId.Add("DFS", ConfigurationFactory.Instance.Configuration().GeoserverWfsDealIdCellDFS);
            cellId.Add("Portfolio", ConfigurationFactory.Instance.Configuration().GeoserverWfsDealIdCellPortfolio);
            cellId.Add("PortfolioInitial", ConfigurationFactory.Instance.Configuration().GeoserverWfsDealIdCellPortfolioInitial);

            return cellId;
        }

        private List<string> GetPopupDealIds(GeoLayerParams geoLayer)
        {
            List<string> dealIds = new List<string>();

            string layers = null;

            string layerObject = GetLayerObject()[geoLayer.QUERY_LAYERS];

            StringBuilder sb = new StringBuilder();


            try
            {
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

                if (layerObject.ToLower().IndexOf("ma") != -1)
                {
                    doc = iMaGeoService.GetLayerDocOnClick(geoLayer);
                }
                else if (layerObject.ToLower().IndexOf("dfs") != -1)
                {
                    doc = iDfsGeoService.GetLayerDocOnClick(geoLayer);

                }
                else if (layerObject.ToLower().IndexOf("initial") != -1)
                {
                    doc = iPortGeoService.GetLayerDocOnClick(geoLayer);
                }
                else
                {
                    doc = iPortGeoService.GetLayerDocOnClick(geoLayer);
                }
                //

                HtmlAgilityPack.HtmlNodeCollection rows = doc.DocumentNode.SelectNodes("//table//tr");

                Dictionary<string, string> dealIdList = new Dictionary<string, string>();

                StringBuilder tableResults = new StringBuilder();

                int idNumber = GetCellIdFromLayerObject()[layerObject];

                //foreach (var item in rows)
                //{
                //    LogFactory.Instance.Log().GenerateEmail("GeoserverRequest", item.InnerHtml);
                //}

                for (int i = 1; i < rows.Count + 1; i++)
                {
                    string dealId = null;



                    try
                    {
                        string xpathToDealId = "/html[1]/body[1]/table[1]/tr[" + (i + 1).ToString() + "]/td[" + idNumber.ToString() + "]";
                        dealId = doc.DocumentNode.SelectSingleNode(xpathToDealId).InnerText;
                        //dealId = rows[i + 1].SelectSingleNode("//td[" + idNumber.ToString() + "]").InnerText;

                        if (!dealIds.Contains(dealId))
                        {
                            dealIds.Add(dealId);
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }
                sb.AppendLine("Layers: " + layers).AppendLine();


            }
            catch (Exception ex)
            {
               // Infrastructure.Logging.LogFactory.Instance.Log().GenerateEmail("Error Getting GetPopupDealIds", ex.Message);
            }

            return dealIds;
        }

        private string MaPopupDetails(GeoLayerParams geoLayer, List<string> dealIds)
        {
            List<Data.IMapIndexData> ma = iService.GetMaData();
            List<MaData> vals = CollectionHelpers.CollectionHelper<MaData>.GetProperList(ma);
            vals = vals.Where(s => dealIds.Contains(s.id)).ToList();
            //vals = vals.Where(s => s != null).Take(5).ToList();

            StringBuilder sb = new StringBuilder();
            StringBuilder ul = new StringBuilder();
            sb.Append("<div>");
            ul.Append("<ul style='margin-left:22% !important;' class='pagination'>");
            int i = 1;
            foreach (var item in vals)
            {
                if (i != 1)
                {
                    sb.Append("<div style='display:none;' id='popupData_").Append(i).Append("'>").AppendLine();
                }
                else
                {
                    sb.Append("<div id='popupData_").Append(i).Append("'>").AppendLine();
                }
                sb.Append(iMaDataGeoService.GeneratePopup(item, ConfigurationFactory.Instance.Configuration().CacheKeyMaPopup + "_" + "ID" + "_" + item.id, ConfigurationFactory.Instance.Configuration().CacheKeyMaPopupPath)).AppendLine();

                ul.Append("<li class='page-item'><a style='color:" + item.HexColor + " !important;font-weight:bold;' class='page-link' onclick='ShowPopup(" + i + "," + vals.Count + 1 + ")'").Append(">" + i + "</a>").Append("</li>");
                sb.Append("</div>");
                i++;
            }

            ul.Append("</ul>").AppendLine();
            string result = sb.ToString() + ul.ToString();
            result += "</div>";

            //string popup = GeoserverHelpers.GeoserverPopupHelper.GeneratePopupPortfolio(vals.FirstOrDefault());

            return result;
        }

        private string DfsPopupDetails(GeoLayerParams geoLayer, List<string> dealIds)
        {
            List<Data.IMapIndexData> dfs = iDfsService.GetDfsData();
            List<DfsData> vals = CollectionHelpers.CollectionHelper<DfsData>.GetProperList(dfs);
            vals = vals.Where(s => dealIds.Contains(s.theId)).ToList();
            //vals = vals.Take(5).ToList();

            StringBuilder sb = new StringBuilder();
            StringBuilder ul = new StringBuilder();
            sb.Append("<div>");
            ul.Append("<ul style='margin-left:22% !important;' class='pagination'>");
            int i = 1;
            foreach (var item in vals)
            {
                if (i != 1)
                {
                    sb.Append("<div style='display:none;' id='popupData_").Append(i).Append("'>").AppendLine();
                }
                else
                {
                    sb.Append("<div id='popupData_").Append(i).Append("'>").AppendLine();
                }

                sb.Append(iMaDataGeoService.GeneratePopup(item, ConfigurationFactory.Instance.Configuration().CacheKeyDfsPopup + "_" + "ID" + "_" + item.theId, ConfigurationFactory.Instance.Configuration().CacheKeyDfsPopupPath)).AppendLine();

                ul.Append("<li class='page-item'><a style='color:" + item.HexColor + " !important;font-weight:bold;' class='page-link' onclick='ShowPopup(" + i + "," + vals.Count + 1 + ")'").Append(">" + i + "</a>").Append("</li>");
                sb.Append("</div>");
                i++;
            }

            ul.Append("</ul>").AppendLine();
            string result = sb.ToString() + ul.ToString();
            result += "</div>";

            //string popup = GeoserverHelpers.GeoserverPopupHelper.GeneratePopupPortfolio(vals.FirstOrDefault());

            return result;
        }

        private string PortfolioPopupDetails(GeoLayerParams geoLayer, List<string> dealIds)
        {
            List<Data.IMapIndexData> port = iPortfolioService.GetData();
            List<PortfolioData> vals = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(port);
            vals = vals.Where(s => dealIds.Contains(s.CompanyId)).ToList();
            //vals = vals.Where(s => s.Logo != "").Take(5).ToList();

            StringBuilder sb = new StringBuilder();
            StringBuilder ul = new StringBuilder();
            sb.Append("<div>");
            ul.Append("<ul style='margin-left:22% !important;' class='pagination'>");
            int i = 1;
            foreach (var item in vals)
            {
                if (i != 1)
                {
                    sb.Append("<div style='display:none;' id='popupData_").Append(i).Append("'>").AppendLine();
                }
                else
                {
                    sb.Append("<div id='popupData_").Append(i).Append("'>").AppendLine();
                }
                sb.Append(iMaDataGeoService.GeneratePopup(item, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioPopup + "_" + "ID" + "_" + item.CompanyId, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioPopupPath)).AppendLine();
                ul.Append("<li class='page-item'><a style='color:" + item.HexColor + " !important;font-weight:bold;' class='page-link' onclick='ShowPopup(" + i + "," + vals.Count + 1 + ")'").Append(">" + i + "</a>").Append("</li>");
                sb.Append("</div>");
                i++;
            }

            ul.Append("</ul>").AppendLine();
            string result = sb.ToString() + ul.ToString();
            result += "</div>";

            //string popup = GeoserverHelpers.GeoserverPopupHelper.GeneratePopupPortfolio(vals.FirstOrDefault());

            return result;
        }

        private string CixPopupDetails(PortfolioData port)
        {
            string result = "";

            result = iMaDataGeoService.GeneratePopup(port, ConfigurationFactory.Instance.Configuration().CacheKeyPortfolioPopup + "_" + "ID" + "_" + port.CompanyId + "_" + "CIX", "~/Views/Templates/CixPopup.cshtml");

            return result;
        }

        [ActionName("FetchShapeFiles")]
        [HttpPost]
        public HttpResponseMessage FetchShapeFiles(MapSearch map)
        {
            Models.Authentication.ViewModels.AuthViewModel auth = Infrastructure.Session.AuthSession.GetAuthModel(map);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            //response.Headers.Add("Set-Cookie", "fileDownload=false; path=/");
            try
            {
                if (iAuth.IsUserValid(auth))
                {
                    if (map.WhichDb == "MATAB")
                    {
                        response = MaSourceDocs(map);
                    }
                    else if (map.WhichDb == "DFSTAB")
                    {
                        response = DfsSourceDocs(map);
                    }
                    else if (map.WhichDb == "PORTFOLIOTAB")
                    {
                        response = PortfolioSourceDocs(map);
                    }
                }
            }
            catch (Exception ex)
            {
                LogFactory.Instance.Log().GenerateEmail("Error Zipping Source Docs", ex.Message);
            }
            return response;
        }

        protected HttpResponseMessage ZipContentResult(ZipFile zipFile)
        {

            var pushStreamContent = new PushStreamContent((stream, content, context) =>
            {
                zipFile.Save(stream);
                stream.Close();
            }, "application/zip");

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = pushStreamContent };
        }

        private HttpResponseMessage MaSourceDocs(MapSearch map)
        {

            List<Data.IMapIndexData> ma = iService.GetMaData();

            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            if (hasFilters)
            {
                ma = iService.GetFilteredData(ma, map);
            }

            List<MaData> vals2 = new List<MaData>(ma.Count);

            foreach (IMapIndexData d in ma)
            {
                vals2.Add((MaData)d);
            }

            ZipFile zip = new ZipFile();
            iMaGeoService.GetSourceDocs(ma, map, zip);

            MemoryStream ms = new MemoryStream();
            zip.Name = "MaSelectDealDocs.zip";
            zip.Save(ms);

            return ZipContentResult(zip);

        }

        private HttpResponseMessage PortfolioSourceDocs(MapSearch map)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);

            string cql = GeoserverHelpers.GeoserverCqlFilter.CreateCqlFilter(map);

            //Data.Geoserver.Portfolio.PortfolioGeoserverRepository.DownloadShapeFiles(map);
            string baseUrl = ConfigurationFactory.Instance.Configuration().GeoServerBaseUrl + "MA-DS/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=MA-DS:portfolioGeometryByPlayView&maxFeatures=50&outputFormat=SHAPE-ZIP&CQL_FILTER=" + cql;

            
            byte[] theFile;

            using (var client = new WebClient())
            {
                theFile = client.DownloadData(baseUrl);
            }

            response.Content = new StreamContent(new MemoryStream(theFile));
            response.Content.Headers.ContentType =
      new MediaTypeHeaderValue("application/octet-stream");



            return response;

        }

        private HttpResponseMessage DfsSourceDocs(MapSearch map)
        {

            List<Data.IMapIndexData> dfs = iDfsService.GetDfsData();

            var hasFilters = Infrastructure.FilterChecker.FilterChecker.HasFilters(map);

            if (hasFilters)
            {
                dfs = iDfsService.GetFilteredData(dfs, map);
            }

            List<DfsData> vals2 = new List<DfsData>(dfs.Count);

            foreach (IMapIndexData d in dfs)
            {
                vals2.Add((DfsData)d);
            }


            ZipFile zip = new ZipFile();
            iDfsGeoService.GetSourceDocs(dfs, map, zip);

            MemoryStream ms = new MemoryStream();
            zip.Name = "MaSelectDealDocs.zip";
            zip.Save(ms);

            return ZipContentResult(zip);
        }

        [ActionName("WfsRequestPopup")]
        [HttpPost]
        public IHttpActionResult WfsRequestPopup(GeoLayerParams datObject)
        {
            List<string> dealIds = GetPopupDealIds(datObject);

            dynamic result = null;

            if (datObject.Layers.IndexOf("listing") != -1)
            {
                result = DfsPopupDetails(datObject, dealIds);
            }
            else if(datObject.Layers.IndexOf("maGeometry") != -1)
            {
                result = MaPopupDetails(datObject, dealIds);
            }

            else
            {
                result = PortfolioPopupDetails(datObject, dealIds);
            }
            return Ok(result);
        }

        public class Cix
        {
            public string CompanyId { get; set; }
        }

        [ActionName("GenerateCixPopup")]
        [HttpPost]
        public IHttpActionResult GenerateCixPopup(Cix company)
        {
            List<Data.IMapIndexData> port = iPortfolioService.GetData();
            List<PortfolioData> vals = CollectionHelpers.CollectionHelper<PortfolioData>.GetProperList(port);
            PortfolioData response = vals.Where(s => company.CompanyId == s.CompanyId).FirstOrDefault();
            string result = CixPopupDetails(response);
            return Ok(result);
        }
    }
}
