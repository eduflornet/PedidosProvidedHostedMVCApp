using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PedidosProvidedHostedMVCAppWeb.Models;

namespace PedidosProvidedHostedMVCAppWeb.Controllers
{
    public class HomeController : Controller
    {
        [SharePointContextFilter]
        public ActionResult Index()
        {
            User spUser = null;

            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    spUser = clientContext.Web.CurrentUser;

                    clientContext.Load(spUser, user => user.Title);

                    clientContext.ExecuteQuery();

                    ViewBag.UserName = spUser.Title;
                }
            }

            return View();
        }

        public ActionResult TotalPedidos()
        {
            object model = null;
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web);
                    clientContext.ExecuteQuery();

                    ListCollection lists = web.Lists;
                    clientContext.Load<ListCollection>(lists);
                    clientContext.ExecuteQuery();



                    var pedidos = lists.GetByTitle("Pedidos");
                    clientContext.Load(pedidos);
                    var productos = lists.GetByTitle("Productos");
                    clientContext.Load(pedidos);
                    clientContext.Load(productos);


                    clientContext.ExecuteQuery();
                    CamlQuery pedidosQuery = new CamlQuery();


                    ListItemCollection pedidosItems = pedidos.GetItems(pedidosQuery);
                    clientContext.Load(pedidosItems);
                    clientContext.ExecuteQuery();


                    var total = 0.0;
                    var clientes = new Dictionary<string, double>();


                    foreach (var pedidosItem in pedidosItems)
                    {
                        FieldLookupValue lookup = pedidosItem["Producto"] as FieldLookupValue;
                        int lId = lookup.LookupId;
                        var uds = pedidosItem["Unidades"];
                        var pi = productos.GetItemById(lId);
                        clientContext.Load(pi);
                        clientContext.ExecuteQuery();
                        var precio = pi["Precio"];
                        var venta = (double) precio*(double) uds;
                        total += venta;

                        if (clientes.ContainsKey(pedidosItem["Title"].ToString()))
                        {
                            clientes[pedidosItem["Title"].ToString()] = clientes[pedidosItem["Title"].ToString()] +
                                                                        venta;

                        }
                        else
                        {
                            clientes.Add(pedidosItem["Title"].ToString(), venta);
                        }

                    }

                    var mc = total/clientes.Keys.Count;

                    model = new Totales() {Numero = pedidosItems.Count, MediaCliente = mc, Total = total};

                }
            }
            
            return View(model);
        }

        public ActionResult ListaPedidos()
        {
            List<Pedidos> model = new List<Pedidos>();
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web);
                    clientContext.ExecuteQuery();

                    ListCollection lists = web.Lists;
                    clientContext.Load<ListCollection>(lists);
                    clientContext.ExecuteQuery();



                    var pedidos = lists.GetByTitle("Pedidos");
                    clientContext.Load(pedidos);
                    var productos = lists.GetByTitle("Productos");
                    clientContext.Load(pedidos);
                    clientContext.Load(productos);


                    clientContext.ExecuteQuery();
                    CamlQuery pedidosQuery = new CamlQuery();


                    ListItemCollection pedidosItems = pedidos.GetItems(pedidosQuery);
                    clientContext.Load(pedidosItems);

                    clientContext.ExecuteQuery();

                    foreach (var pedidosItem in pedidosItems)
                    {
                        FieldLookupValue lookup = pedidosItem["Producto"] as FieldLookupValue;
                        int lId = lookup.LookupId;
                        int uds;
                        int.TryParse(pedidosItem["Unidades"].ToString(), out uds);
                        var pi = productos.GetItemById(lId);
                        clientContext.Load(pi);
                        clientContext.ExecuteQuery();
                        var precio = pi["Precio"];
                        var venta = (double)precio * (double)uds;




                        model.Add(new Pedidos()
                        {
                            Cliente = pedidosItem["Title"].ToString(),
                            Pedido = pi["Title"].ToString(),
                            Unidades = uds,
                            Total = venta
                        });


                    }

                }
                return View(model);
            }
        }

        public ActionResult Add()
        {
            var prodList = new List<Productos>();

            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web);
                    clientContext.ExecuteQuery();

                    ListCollection lists = web.Lists;
                    clientContext.Load<ListCollection>(lists);
                    clientContext.ExecuteQuery();
                    
                    var productos = lists.GetByTitle("Productos");
                    clientContext.Load(productos);


                    clientContext.ExecuteQuery();
                    CamlQuery productosQuery = new CamlQuery();


                    ListItemCollection productosItems = productos.GetItems(productosQuery);
                    clientContext.Load(productosItems);

                    clientContext.ExecuteQuery();
                    
                    foreach (var productoItem in productosItems)
                    {
                        int id;
                        int.TryParse(productoItem["ID"].ToString(), out id);
                        
                        prodList.Add(new Productos()
                        {
                            Id = id,
                            Nombre = productoItem["Title"].ToString()
                        });


                    }
                 }
              }

            ViewBag.idProducto = new SelectList(prodList, "Id", "Nombre");
            return View(new Pedidos());

        }

        [HttpPost]
        public ActionResult Add(Pedidos model)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    Web web = clientContext.Web;
                    clientContext.Load(web);
                    clientContext.ExecuteQuery();

                    ListCollection lists = web.Lists;
                    clientContext.Load<ListCollection>(lists);
                    clientContext.ExecuteQuery();

                    var pedidos = lists.GetByTitle("Pedidos");
                    clientContext.Load(pedidos);

                    ListItemCreationInformation listCreationInformation = new ListItemCreationInformation();
                    ListItem oListItem = pedidos.AddItem(listCreationInformation);
                    oListItem["Title"] = model.Cliente;
                    oListItem["Unidades"] = model.Unidades;
                    oListItem["Fecha"] = DateTime.Now;
                    var lv = new FieldLookupValue { LookupId = model.idProducto };
                    oListItem["Producto"] = lv;
                    
                    oListItem.Update();
                    clientContext.ExecuteQuery();


                }

                return RedirectToAction("Index", new { SPHostUrl = SharePointContext.GetSPHostUrl(HttpContext.Request).AbsoluteUri }); ;

            }
        }

    }

}

    


