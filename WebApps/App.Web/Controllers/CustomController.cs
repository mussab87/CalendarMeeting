using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace App.Web.Controllers;

//[PermissionAuthorize]
public class CustomController : BaseController
{
    private readonly ILogger<AdminController> _logger;
    public CustomController(IServiceProvider serviceProvider, ILogger<AdminController> logger) : base(serviceProvider)
    {
        _logger = logger;
    }

    /// <summary>
    /// Bootstrap Features
    /// </summary>
    /// <returns></returns>
    #region HijriDate  
    public IActionResult HijriDate()
    {

        return View();
    }
    #endregion

    #region Typography  
    public IActionResult Typography()
    {
        return View();
    }
    #endregion

    #region Buttons  
    public IActionResult Buttons()
    {
        return View();
    }
    #endregion

    #region ButtonGgroup  
    public IActionResult ButtonGgroup()
    {
        return View();
    }
    #endregion

    #region Dropdown  
    public IActionResult Dropdown()
    {
        return View();
    }
    #endregion

    #region Tables  
    public IActionResult Tables()
    {
        return View();
    }
    #endregion

    #region Modal  
    public IActionResult Modal()
    {
        return View();
    }
    #endregion

    #region Alerts  
    public IActionResult Alerts()
    {
        return View();
    }
    #endregion

    #region Popover  
    public IActionResult Popover()
    {
        return View();
    }
    #endregion

    #region Tooltip  
    public IActionResult Tooltip()
    {
        return View();
    }
    #endregion


    /// <summary>
    /// Custom Features
    /// </summary>
    /// <returns></returns>
    #region Utilities  
    public IActionResult Utilities()
    {
        return View();
    }
    #endregion

    #region Accordions  
    public IActionResult Accordions()
    {
        return View();
    }
    #endregion

    #region Label  
    public IActionResult Label()
    {
        return View();
    }
    #endregion

    #region Pulse  
    public IActionResult Pulse()
    {
        return View();
    }
    #endregion

    #region LineTabs  
    public IActionResult LineTabs()
    {
        return View();
    }
    #endregion

    #region AdvanceNavs  
    public IActionResult AdvanceNavs()
    {
        return View();
    }
    #endregion

    #region Timeline  
    public IActionResult Timeline()
    {
        return View();
    }
    #endregion

    #region Pagination  
    public IActionResult Pagination()
    {
        return View();
    }
    #endregion

    #region Symbol  
    public IActionResult Symbol()
    {
        return View();
    }
    #endregion

    #region Spinners  
    public IActionResult Spinners()
    {
        return View();
    }
    #endregion

    #region Iconbox  
    public IActionResult Iconbox()
    {
        return View();
    }
    #endregion

    #region Callout  
    public IActionResult Callout()
    {
        return View();
    }
    #endregion

    #region Ribbons  
    public IActionResult Ribbons()
    {
        return View();
    }
    #endregion



    /// <summary>
    /// Icons Features
    /// </summary>
    /// <returns></returns>
    #region SVGIcons  
    public IActionResult SVGIcons()
    {
        return View();
    }
    #endregion

    #region CustomIcons  
    public IActionResult CustomIcons()
    {
        return View();
    }
    #endregion

    #region Flaticon  
    public IActionResult Flaticon()
    {
        return View();
    }
    #endregion

    #region Fontawesome5  
    public IActionResult Fontawesome5()
    {
        return View();
    }
    #endregion

    #region Lineawesome  
    public IActionResult Lineawesome()
    {
        return View();
    }
    #endregion

    #region Socicons  
    public IActionResult Socicons()
    {
        return View();
    }
    #endregion



    /// <summary>
    /// Cards Features
    /// </summary>
    /// <returns></returns>
    #region GeneralCards  
    public IActionResult GeneralCards()
    {
        return View();
    }
    #endregion

    #region StackedCards  
    public IActionResult StackedCards()
    {
        return View();
    }
    #endregion

    #region TabbedCards  
    public IActionResult TabbedCards()
    {
        return View();
    }
    #endregion

    #region DraggableCards  
    public IActionResult DraggableCards()
    {
        return View();
    }
    #endregion

    #region CardsTools  
    public IActionResult CardsTools()
    {
        return View();
    }
    #endregion

    #region StickyCards  
    public IActionResult StickyCards()
    {
        return View();
    }
    #endregion

    #region StretchedCards  
    public IActionResult StretchedCards()
    {
        return View();
    }
    #endregion


    /// <summary>
    /// Widgets Features
    /// </summary>
    /// <returns></returns>
    #region Lists  
    public IActionResult Lists()
    {
        return View();
    }
    #endregion

    #region Stats  
    public IActionResult Stats()
    {
        return View();
    }
    #endregion

    #region Charts  
    public IActionResult Charts()
    {
        return View();
    }
    #endregion

    #region Mixed  
    public IActionResult Mixed()
    {
        return View();
    }
    #endregion

    #region Feeds  
    public IActionResult Feeds()
    {
        return View();
    }
    #endregion

    #region Engage  
    public IActionResult Engage()
    {
        return View();
    }
    #endregion

    #region BaseTables  
    public IActionResult BaseTables()
    {
        return View();
    }
    #endregion

    #region AdvanceTables  
    public IActionResult AdvanceTables()
    {
        return View();
    }
    #endregion

    #region NavPanels  
    public IActionResult NavPanels()
    {
        return View();
    }
    #endregion


    /// <summary>
    /// Calendar Features
    /// </summary>
    /// <returns></returns>
    #region ListViews  
    public IActionResult ListViews()
    {
        return View();
    }
    #endregion

    #region ExternalEvents  
    public IActionResult ExternalEvents()
    {
        return View();
    }
    #endregion


    /// <summary>
    /// Charts Features
    /// </summary>
    /// <returns></returns>
    #region Apexcharts  
    public IActionResult Apexcharts()
    {
        return View();
    }
    #endregion

    #region FlotCharts  
    public IActionResult FlotCharts()
    {
        return View();
    }
    #endregion



    /// <summary>
    /// Miscellaneous Features
    /// </summary>
    /// <returns></returns>
    #region KanbanBoard  
    public IActionResult KanbanBoard()
    {
        return View();
    }
    #endregion

    #region StickyPanels  
    public IActionResult StickyPanels()
    {
        return View();
    }
    #endregion

    #region BlockUI  
    public IActionResult BlockUI()
    {
        return View();
    }
    #endregion

    #region PerfectScrollbar  
    public IActionResult PerfectScrollbar()
    {
        return View();
    }
    #endregion

    #region TreeView  
    public IActionResult TreeView()
    {
        return View();
    }
    #endregion

    #region BootstrapNotify  
    public IActionResult BootstrapNotify()
    {
        return View();
    }
    #endregion

    #region Toastr  
    public IActionResult Toastr()
    {
        return View();
    }
    #endregion

    #region SweetAlert2  
    public IActionResult SweetAlert2()
    {
        return View();
    }
    #endregion

    #region DualListbox  
    public IActionResult DualListbox()
    {
        return View();
    }
    #endregion

    #region SessionTimeout  
    public IActionResult SessionTimeout()
    {
        return View();
    }
    #endregion
    

    #region Cropper  
    public IActionResult Cropper()
    {
        return View();
    }
    #endregion

}
