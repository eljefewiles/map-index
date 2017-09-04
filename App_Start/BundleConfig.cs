using System.Web;
using System.Web.Optimization;
using MapIndex.BundleHelpers;

namespace MapIndex.App_Start
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            //bundles.UseCdn = true;

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
              "~/Packages/Scripts/jquery.js").WithLastModifiedToken());


            bundles.Add(new StyleBundle("~/fontawesome/css").Include(
            "~/Packages/CSS/fontawesome.css").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/angular").Include(
                           "~/Packages/Scripts/angular.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                       "~/Packages/Scripts/dataTables.js").WithLastModifiedToken());

            bundles.Add(new StyleBundle("~/leaflet/css").Include(
                 "~/Packages/CSS/leaflet.css"
             ).WithLastModifiedToken());
            bundles.Add(new ScriptBundle("~/bundles/leaflet").Include(
           "~/Packages/Scripts/leaflet.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
       "~/Scripts/bootstrap.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/leafletcluster").Include(
     "~/Packages/Scripts/leafletmarkerclustersrc.js").WithLastModifiedToken());


            bundles.Add(new ScriptBundle("~/bundles/moment").Include(
     "~/Packages/Scripts/moment.js").WithLastModifiedToken());


            bundles.Add(new StyleBundle("~/bootstrap/css").Include(
                 "~/Packages/CSS/bootstrap.css"
             ).WithLastModifiedToken());

            bundles.Add(new StyleBundle("~/bootstrapdatatables/css").Include(
                "~/Packages/CSS/datatablesbootstrap.css"
            ).WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/angularuigrid").Include(
                     "~/Packages/Scripts/angularuigrid.js").WithLastModifiedToken());

            bundles.Add(new StyleBundle("~/angulargrid/css").Include(
              "~/Packages/CSS/angularuigrid.css"
          ).WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                    "~/Packages/Scripts/jqueryui.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/angularuibootstrap").Include(
              "~/Packages/Scripts/angularuibootstrap.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/angularchecklist").Include(
            "~/ScriptPlugins/angularchecklistmodel.js").WithLastModifiedToken());

            bundles.Add(new StyleBundle("~/Content/css").Include(
                    "~/Content/main.css",
                    "~/Content/mamap.css",
                    "~/Content/grid.css",
                    "~/Content/angulardatatables.css",
                    "~/Content/Site.css"
                    ).WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/spinner").Include(
                    "~/Scripts/spinner.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/highcharts").Include(
                "~/Packages/Scripts/highcharts.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/highstock").Include(
         "~/Packages/Scripts/highstock.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/downloadplugin").Include(
       "~/Packages/Scripts/downloadplugin.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/angulardatatables").Include(
 "~/Packages/Scripts/angulardatatables.js").WithLastModifiedToken());


            bundles.Add(new StyleBundle("~/scroller/css").Include(
           "~/Packages/CSS/datatablesscroller.css"
       ).WithLastModifiedToken());
            bundles.Add(new ScriptBundle("~/bundles/ngcsv").Include(
     "~/ng-csv/ng-csv.js").WithLastModifiedToken());
            bundles.Add(new ScriptBundle("~/bundles/mapindexapp").Include(
                        "~/Scripts/mapindex/app/MapApp.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/mapindexconfiguration").Include(
                "~/Scripts/mapindex/configuration/configmanager.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/mapindexdata").Include(
               "~/Scripts/mapindex/data/maprepository.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/mapindexdirectives").IncludeDirectory(
              "~/Scripts/mapindex/directives", "*.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/mapindexfactories").IncludeDirectory(
            "~/Scripts/mapindex/factories", "*.js").WithLastModifiedToken());

            bundles.Add(new ScriptBundle("~/bundles/mapindexcontrollers").Include(
            "~/Scripts/mapindex/controllers/mapindexcontroller.js").WithLastModifiedToken());

       
            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            BundleTable.EnableOptimizations = true;
        }
    }
}