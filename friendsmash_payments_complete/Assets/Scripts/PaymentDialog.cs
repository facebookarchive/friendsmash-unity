using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Facebook.MiniJSON;
using Facebook;
using System;


public class PaymentDialog : MonoBehaviour 
{

    //   Inspector tunable members   //


	public bool DialogEnabled = true;
	public GUISkin MenuSkin;
	private MainMenu mainMenu;
	
	public Texture PanelHeader;  // Textures for dialog
	public Texture PanelBody;
	public Texture PanelFooter;
	public float   PanelHeight;  // Height of dialog
	
	
	public Vector2 CoinsButtonPos; // Position of coin button
	public Vector2 ItemsButtonPos; // Position of items button

	public Vector2 InitialPriceItemPos; // Position of first item in price list
	public Vector2 PriceItemPosDelta;	// Step between each item in price list
	public Vector2 PriceItemSize;		// Size of price item in price list

	public Vector2 PriceItemFirstIconPos;  // Position of first icon in price item
	public Vector2 PriceItemSecondIconPos; // Position of second icon in price item
	public float PriceItemSecondTextPos;   // Offset of second text within price item


	public Texture PriceItemCoinTexture;  // Icons for coins, bombs and lives
	public Texture PriceItemBombTexture;  
	public Texture PriceItemLifeTexture;

	public Texture PayWithMobileTexture;  // Button texture for pay by mobile button
	public Vector2 PayWithMobilePos;      // Position of pay by mobile button
	public Rect PayByMobileTextRect;	  // Position of text for pay by mobile button
	public Rect PayByMobileRect;		  // Position of clickable area for pay by mobile button



	public Rect CloseButtonRect; 	// Position of close dialog button
	public Rect BombsRect; 			// Positions of labels for current inventory
	public Rect LivesRect;
	public Rect CoinsRect;


    //   Private members   //

	Vector2 priceItemPos;    // Position of next price item to be drawn
	Vector3 dialogPosition;  // Position of topleft corner of dialog


	string userCurrency;					// User currency data returned from Facebook
	double userCurrencyUSDExchangeInverse;

	bool haveUserCurrency = false;			// Variables to keep track of whether asynchrously requested data has 
	bool haveMobilePricePoints = false;		// been returned
	int  numCoinPackagesReturned = 0;

	bool localPricesCalculated = false;		// Set true once prices have been calculated for the local currency
	
	enum TabSelected
	{
		PRODUCTS_TAB,
		COINS_TAB
	};
	TabSelected tabSelected = TabSelected.PRODUCTS_TAB; // The currently selected dialog tab

	bool mobilePaymentsTab;					// Whether we are currently showing mobile price points

	//  CoinPackage represents a package of in-game coins that can be purchased with real world money
	struct CoinPackage
	{
		public string url;							// URL where information about this package is hosted
		public int numCoins;						// Number of in-game coins this package grants the user
		public string objectID;						// Graph API ID of this package
		public Dictionary<string,double> price;		// Price of this package in various currencies
		public double localPrice;					// Price in the user's local currency
		public MobilePricePoint mobilePricePoint;	// The mobile price point to use for this package when offering mobile shortcutting
		public bool valid;							// Whether this data had been set based on data from URL
	}

	CoinPackage[] coinPackages = new CoinPackage[]
	{
		new CoinPackage {numCoins = 10 ,objectID = "591282904281878"},
		new CoinPackage {numCoins = 25 ,objectID = "239124136259344"},
		new CoinPackage {numCoins = 50 ,objectID = "362954223847641"},
		new CoinPackage {numCoins = 100,objectID = "686215168084245"}
	};

	CoinPackage activeCoinPackage;    // Package for which a transaction is currently is process

	struct MobilePricePoint 
	{
		public string id;				// pricepoint ID
		public double payerAmount;		// amount player pays
		public double payoutAmount;		// amount developer receives (before Facebook 30% rev share)
		public string country;
		public string currency;
	}
	List<MobilePricePoint> mobilePricePoints = new List<MobilePricePoint>();

	const int NO_MOBILE_PRICE_POINT = -1;
	
	struct CurrencyData 
	{
		public string symbol;
		public bool pre;
	};

	Dictionary<string,CurrencyData> currencyDataTable = new Dictionary<string,CurrencyData>();



	enum Commodity
	{
		LIVES = 0,
		BOMBS = 1
	}

	// ProductData represents a package of in-game items that can be bought with in-game coins
	struct ProductData 
	{
		public Commodity commodity;
		public int coinPrice; // price in in-game coins
		public int quantity;  // amount of in game lives/coins received in this package
		public ProductData(Commodity commodity, int coinPrice, int quantity)
		{
			this.commodity = commodity;
			this.coinPrice = coinPrice;
			this.quantity = quantity;
		}

		public string MakeProductString ()
		{
			if (commodity == Commodity.LIVES)
			{
				return string.Format("{0} Lives",quantity);
			}
			else if (commodity == Commodity.BOMBS)
			{
				return string.Format("{0} Bombs",quantity);
			}
			else return "?";
		}
	}

	List<ProductData> productData = new List<ProductData>();

	
	void SetupProducts() 
	{
		// Configure in game product packages
		productData.Add(new ProductData(Commodity.LIVES, 5,10)); // For 5 coins the player can buy 10 lives
		productData.Add(new ProductData(Commodity.LIVES, 12,20)); 
		productData.Add(new ProductData(Commodity.LIVES, 25,40));
		productData.Add(new ProductData(Commodity.BOMBS, 5,15));
		productData.Add(new ProductData(Commodity.BOMBS, 12,20));
		productData.Add(new ProductData(Commodity.BOMBS, 25,40));
	}

	void SetupCurrencyData() 
	{
		// Hard-coded data for how to render each currency symbol

		currencyDataTable["USD"] = new CurrencyData{symbol= "\x24",    pre= true};
		currencyDataTable["SGD"] = new CurrencyData{symbol= "S\x24",   pre= true};
		currencyDataTable["RON"] = new CurrencyData{symbol= "LEU",      pre= false};
		currencyDataTable["EUR"] = new CurrencyData{symbol= "\x20ac",  pre= true};
		currencyDataTable["TRY"] = new CurrencyData{symbol= "\x20ba",  pre= true};
		currencyDataTable["SEK"] = new CurrencyData{symbol= "kr",       pre= false};
		currencyDataTable["ZAR"] = new CurrencyData{symbol= "R",        pre= true};
		currencyDataTable["BHD"] = new CurrencyData{symbol= "BD",       pre= true};
		currencyDataTable["HKD"] = new CurrencyData{symbol= "HK\x24",  pre= true};
		currencyDataTable["CHF"] = new CurrencyData{symbol= "Fr.",      pre= false};
		currencyDataTable["NIO"] = new CurrencyData{symbol= "C\x24",   pre= true};
		currencyDataTable["JPY"] = new CurrencyData{symbol= "\xa5",   pre= true};
		currencyDataTable["ISK"] = new CurrencyData{symbol= "kr;",      pre= false};
		currencyDataTable["TWD"] = new CurrencyData{symbol= "NT\x24",  pre= true};
		currencyDataTable["NZD"] = new CurrencyData{symbol= "NZ\x24",  pre= true};
		currencyDataTable["CZK"] = new CurrencyData{symbol= "K\x010d;",  pre= true};
		currencyDataTable["AUD"] = new CurrencyData{symbol= "A\x24",   pre= true};
		currencyDataTable["THB"] = new CurrencyData{symbol= "\x0e3f",  pre= true};
		currencyDataTable["BOB"] = new CurrencyData{symbol= "Bs",       pre= true};
		currencyDataTable["BRL"] = new CurrencyData{symbol= "B\x24",   pre= true};
		currencyDataTable["MXN"] = new CurrencyData{symbol= "Mex\x24", pre= true};
		currencyDataTable["ILS"] = new CurrencyData{symbol= "\x20aa",  pre= true};
		currencyDataTable["JOD"] = new CurrencyData{symbol= "JD",       pre= false};
		currencyDataTable["HNL"] = new CurrencyData{symbol= "L",        pre= true};
		currencyDataTable["MOP"] = new CurrencyData{symbol= "MOP\x24", pre= true};
		currencyDataTable["COP"] = new CurrencyData{symbol= "\x24",    pre= true};
		currencyDataTable["UYU"] = new CurrencyData{symbol= "\x24U",   pre= true};
		currencyDataTable["CRC"] = new CurrencyData{symbol= "\x20a1",  pre= true};
		currencyDataTable["DKK"] = new CurrencyData{symbol= "kr",       pre= false};
		currencyDataTable["QAR"] = new CurrencyData{symbol= "QR",       pre= false};
		currencyDataTable["PYG"] = new CurrencyData{symbol= "\x20b2",  pre= true};
		currencyDataTable["EGP"] = new CurrencyData{symbol= "E\xa3",  pre= true};
		currencyDataTable["CAD"] = new CurrencyData{symbol= "C\x24",   pre= true};
		currencyDataTable["LVL"] = new CurrencyData{symbol= "Ls",       pre= true};
		currencyDataTable["INR"] = new CurrencyData{symbol= "\x20b9",  pre= true};
		currencyDataTable["LTL"] = new CurrencyData{symbol= "Lt;",      pre= false};
		currencyDataTable["KRW"] = new CurrencyData{symbol= "\x20a9",  pre= true};
		currencyDataTable["GTQ"] = new CurrencyData{symbol= "Q",        pre= true};
		currencyDataTable["AED"] = new CurrencyData{symbol= "AED",      pre= false};
		currencyDataTable["VEF"] = new CurrencyData{symbol= "Bs.F.",    pre= true};
		currencyDataTable["SAR"] = new CurrencyData{symbol= "SR",       pre= false};
		currencyDataTable["NOK"] = new CurrencyData{symbol= "kr",       pre= false};
		currencyDataTable["UAH"] = new CurrencyData{symbol= "\x20b4",  pre= true};
		currencyDataTable["DOP"] = new CurrencyData{symbol= "RD\x24",  pre= true};
		currencyDataTable["CNY"] = new CurrencyData{symbol= "\xa5",   pre= true};
		currencyDataTable["BGN"] = new CurrencyData{symbol= "lev",      pre= false};
		currencyDataTable["ARS"] = new CurrencyData{symbol= "\x24",    pre= true};
		currencyDataTable["PLN"] = new CurrencyData{symbol= "z\x0142",  pre= false};
		currencyDataTable["GBP"] = new CurrencyData{symbol= "\xa3",   pre= true};
		currencyDataTable["PEN"] = new CurrencyData{symbol= "S/.",      pre= false};
		currencyDataTable["PHP"] = new CurrencyData{symbol= "PhP",      pre= false};
		currencyDataTable["VND"] = new CurrencyData{symbol= "\x20ab",  pre= false};
		currencyDataTable["RUB"] = new CurrencyData{symbol= "py\x0431",pre= false};
		currencyDataTable["RSD"] = new CurrencyData{symbol= "RSD",      pre= false};
		currencyDataTable["HUF"] = new CurrencyData{symbol= "Ft",       pre= false};
		currencyDataTable["MYR"] = new CurrencyData{symbol= "RM",       pre= true};
		currencyDataTable["CLP"] = new CurrencyData{symbol= "\x24",    pre= true};
		currencyDataTable["HRK"] = new CurrencyData{symbol= "kn",       pre= false};
		currencyDataTable["IDR"] = new CurrencyData{symbol= "Rp",       pre= true};
	}
	

	
	// Use this for initialization
	void Start () 
	{
		mainMenu = (MainMenu)gameObject.GetComponent("MainMenu");
		SetupCurrencyData();
		SetupProducts();
	}

	public void OnLoggedIn()
	{
		GetPackagePrices();
		GetUserCurrency();
		GetMobilePricePoints();
	}

	

	void GetUserCurrency() 
	{
		FB.API("/me/?fields=currency", Facebook.HttpMethod.GET, delegate(FBResult response) 
		{
			if (string.IsNullOrEmpty(response.Error) && DeserializeUserCurrency(response.Text,out userCurrency, out userCurrencyUSDExchangeInverse)) 
			{
				Util.Log("Have user currency");
				haveUserCurrency = true;
			  	CheckIfHaveAllPaymentData();
			}
			else
			{
				Util.Log("Error retrieving user currency");
			}
	    });
	}

	
	void GetMobilePricePoints() 
	{
		FB.API("/me/?fields=payment_mobile_pricepoints", Facebook.HttpMethod.GET, delegate(FBResult response) 
		{
			if (string.IsNullOrEmpty(response.Error) && DeserializeMobilePricePoints(response.Text,mobilePricePoints)) 
			{
				haveMobilePricePoints = true;
				Util.Log("Have mobile price points");
				CheckIfHaveAllPaymentData();
			}
			else
			{
				Util.Log("Error retrieving mobile price points");
			}
		});
	}

	FacebookDelegate MakeCoinPackageDelegate(int i)
	{
		return delegate(FBResult response){
    		if (string.IsNullOrEmpty(response.Error) && DeserializeCoinPackage(response.Text, ref coinPackages[i]))
    		{
	    		numCoinPackagesReturned++;
	    		CheckIfHaveAllPaymentData();
	    	}
	    };
	}
	
	void GetPackagePrices() 
	{
		Util.Log("Fetching package prices");
  		for (int i = 0; i < coinPackages.Length; i ++)
  		{
  			coinPackages[i].price = new Dictionary<string,double>();
	  		FB.API(""+coinPackages[i].objectID+"",Facebook.HttpMethod.GET, MakeCoinPackageDelegate(i));
	    }
	}


	
	void CheckIfHaveAllPaymentData() 
	{
  		if (haveUserCurrency && haveMobilePricePoints && numCoinPackagesReturned == coinPackages.Length)
  		{
  			if (localPricesCalculated) 
  			{
  				Util.LogError("local prices already calculated");
  				return;
  			}

  			CalculateLocalPrices();
  			localPricesCalculated = true;
  			SetupMobilePricePoints();
  			
  		}
	}
	

	void CalculateLocalPrices()
	{
		Util.Log("CalculateLocalPrices exchange rate = " + userCurrencyUSDExchangeInverse);
		for (int i = 0; i < coinPackages.Length; i++) 
		{
			double price;
			if (coinPackages[i].price.TryGetValue(userCurrency, out price))
			{
				coinPackages[i].localPrice = price;
			}
			else if (coinPackages[i].price.TryGetValue("USD", out price))
			{
				coinPackages[i].localPrice = price * userCurrencyUSDExchangeInverse;
			} 
			else 
			{
				coinPackages[i].valid = false;
			}
		}
	}

	

	void SetupMobilePricePoints()
	{
		Util.Log("SetupMobilePricePoints");
		for (int i = 0; i < coinPackages.Length; i++) 
		{
			if (coinPackages[i].valid) 
			{
				for (int j = 0; j < mobilePricePoints.Count; j++) 
				{
					if (mobilePricePoints[j].payoutAmount > coinPackages[i].localPrice && mobilePricePoints[j].currency.Equals(userCurrency,StringComparison.OrdinalIgnoreCase))
					{
						coinPackages[i].mobilePricePoint = mobilePricePoints[j];
						break;
					}
				}
			}
		}
	}


	// GUI //
	
	void OnGUI() 
	{
		if (!DialogEnabled)
			return;

		
		GUI.skin = MenuSkin;
		
		// Position the dialog centrally
		dialogPosition = new Vector2((Screen.width - PanelHeader.width)/2,(Screen.height-PanelHeight)/2);
		GUILayout.BeginArea(new Rect(dialogPosition.x,dialogPosition.y,PanelHeader.width,PanelHeight));

		
		// build dialog panel out of header, middle, and footer
		Util.DrawActualSizeTexture(new Vector2(0,0),PanelHeader);
		GUI.DrawTexture(new Rect (0,PanelHeader.height,PanelBody.width,PanelHeight - PanelHeader.height - PanelFooter.height), PanelBody);
		Util.DrawActualSizeTexture(new Vector2(0,PanelHeight-PanelFooter.height),PanelFooter);
		
		// Display current inventory of items
		GUI.Label(BombsRect,string.Format("{0}",mainMenu.NumBombs),MenuSkin.GetStyle("price_item_hover"));
		GUI.Label(LivesRect,string.Format("{0}",mainMenu.NumLives),MenuSkin.GetStyle("price_item_hover"));
		GUI.Label(CoinsRect,string.Format("{0}",mainMenu.CoinBalance),MenuSkin.GetStyle("price_item_hover"));
		

		// draw tab selector buttons
		if (DrawTabButton(ItemsButtonPos,"Products",tabSelected == TabSelected.PRODUCTS_TAB ? "product_tab_selected" : "product_tab"))
			tabSelected = TabSelected.PRODUCTS_TAB;
		if (DrawTabButton(CoinsButtonPos,"Coins",tabSelected == TabSelected.COINS_TAB ? "coins_tab_selected" : "coins_tab"))
		{
			tabSelected = TabSelected.COINS_TAB;
			mobilePaymentsTab = false;
		}
		
		// Draw the currently selected tab
		if (tabSelected == TabSelected.PRODUCTS_TAB)
		{
			DrawProductOptions();
		}
		if (tabSelected == TabSelected.COINS_TAB)
		{
			DrawCoinOptions();
		}
		
		// Close dialog button
		if (GUI.Button (CloseButtonRect,"",MenuSkin.GetStyle("invisible_button")))
			DialogEnabled = false;
		
		GUILayout.EndArea();


		// Make call to draw popup messages here to ensure they are drawn on top of the dialog
		mainMenu.DrawPopupMessage();
		
	}

	bool DrawTabButton(Vector2 pos,string text, string style)
	{
		return GUI.Button(new Rect (pos.x, pos.y, 138, 60), text, MenuSkin.GetStyle(style));	
	}
	
	
	// A convenience function to check if point p is in a guirect r, which is displayed at a certain offset
	bool RectWithOffsetCointainPoint(Rect r, Vector2 p, Vector2 guiOffset)
	{
		Vector3 realP = new Vector3(p.x-guiOffset.x,Screen.height-p.y-guiOffset.y ,0);
		return r.Contains(realP);
	}

	void BeginDrawPriceItems()
	{
		priceItemPos = InitialPriceItemPos;
	}

	bool DrawPriceItem(Texture productIcon, string productDesc, Texture priceIcon, string priceDesc)
	{
		Rect r = new Rect (priceItemPos.x,priceItemPos.y, PriceItemSize.x, PriceItemSize.y);
        
        //draw box
        GUI.Box(r,"",MenuSkin.GetStyle("box"));
		
		 // choose text style based on if the mouse is hovering over this item
		GUIStyle style;
		if (RectWithOffsetCointainPoint(r,Input.mousePosition,dialogPosition))
		{
			style = MenuSkin.GetStyle("price_item_hover");
		}
		else
		{
			style = MenuSkin.GetStyle("price_item");	
		}

		// Draw button and text using chosen style
		bool clicked = GUI.Button(r,"",style);
		GUI.Label(r,productDesc,style);
		r.x += PriceItemSecondTextPos;
		GUI.Label(r,priceDesc,style);
        Util.DrawActualSizeTexture(priceItemPos+PriceItemFirstIconPos,productIcon);
        Util.DrawActualSizeTexture(priceItemPos+PriceItemSecondIconPos,priceIcon);

        //update position for next item
        priceItemPos += PriceItemPosDelta;
     
        return clicked;
	}

	Texture GetTextureFromCommodity(Commodity commodity)
	{
		switch(commodity)
		{
			default:
			case Commodity.LIVES: return PriceItemLifeTexture;
			case Commodity.BOMBS: return PriceItemBombTexture;
		}
	}
	
	void PaymentCallback(FBResult response)
	{
		string paymentId;
		if (string.IsNullOrEmpty(response.Error) && DeserializePaymentResponse(response.Text,out paymentId)) 
		{
			Util.Log("Payment complete");
			Util.Log("Payment id:" + paymentId);
			mainMenu.CoinBalance += activeCoinPackage.numCoins;
			DialogEnabled = false; // hide dialog
			mainMenu.AddPopupMessage("Purchase Complete",2);
		}
		else
		{
			Util.Log("Payment error");
		}
	}
	
	void OnBuyCoins(CoinPackage priceData)
	{
		Util.Log("OnBuyCoins");
		activeCoinPackage = priceData;
		mainMenu.SetFullscreenMode(false);
		FB.Canvas.Pay(priceData.url, "purchaseitem",1, callback:PaymentCallback);
	}

	void OnBuyMobilePricePoint(MobilePricePoint pricePoint, CoinPackage priceData)
	{
		Util.Log("OnBuyCoins mobile priceid = " + pricePoint.id);
		activeCoinPackage = priceData;
		mainMenu.SetFullscreenMode(false);
		FB.Canvas.Pay(priceData.url, "purchaseitem",1, pricepointId:pricePoint.id, callback:PaymentCallback);
		
	}

	void AddProductToBalance(ProductData productData)
	{
		switch(productData.commodity)
		{
			default:
			case Commodity.LIVES: mainMenu.NumLives += productData.quantity; break;
			case Commodity.BOMBS: mainMenu.NumBombs += productData.quantity; break;
		}

	}
	void OnBuyProduct(ProductData productData)
	{
		if (productData.coinPrice <= mainMenu.CoinBalance)
		{
			mainMenu.CoinBalance -= productData.coinPrice;
			AddProductToBalance(productData);
			DialogEnabled = false; // hide dialog
			mainMenu.AddPopupMessage("Purchase Complete",1);
		}
		else
		{
			mainMenu.AddPopupMessage("Not enough coins",1);
		}
	}

	string FormatPrice(double price, CurrencyData currencyData)
	{
		return string.Format(currencyData.pre ? "{0}{1:0.00}" : "{1:0.00}{0}",
							 currencyData.symbol,price);
	}

	void DrawCoinOptions() 
	{
		// Don't show any prices if we haven't recieved all the callbacks yet
		if (!localPricesCalculated)
			return;

		if (mobilePaymentsTab)
		{
			DrawMobilePaymentOptions();
			return;
		}

		BeginDrawPriceItems();
		
		foreach (CoinPackage item in coinPackages)
		{
			if (item.valid)
			{
				if (DrawPriceItem(PriceItemCoinTexture,string.Format ("{0} Coins",item.numCoins), PriceItemCoinTexture, FormatPrice(item.localPrice,currencyDataTable[userCurrency])))
				{
					OnBuyCoins(item);
				}
			}
		}

		if (DrawMobilePaymentButton())
			mobilePaymentsTab = true;
			
	}
	
	void DrawMobilePaymentOptions() 
	{
		BeginDrawPriceItems();
		
		foreach (CoinPackage item in coinPackages)
		{
			if (item.valid && !string.IsNullOrEmpty(item.mobilePricePoint.id))
			{
				if (DrawPriceItem(PriceItemCoinTexture,string.Format ("{0} Coins",item.numCoins), PriceItemCoinTexture, 
							    	FormatPrice(item.mobilePricePoint.payerAmount,currencyDataTable[userCurrency]))){
					OnBuyMobilePricePoint(item.mobilePricePoint,item);
				}
			}
		}
	}
	

	void DrawProductOptions() 
	{
		BeginDrawPriceItems();
		
		foreach (ProductData item in productData)
		{
			string productName = item.MakeProductString();
			if (DrawPriceItem(GetTextureFromCommodity(item.commodity),productName, PriceItemCoinTexture, string.Format ("{0} Coins",item.coinPrice)))
			{
				OnBuyProduct(item);
			}
		}
	}
	
	bool DrawMobilePaymentButton()
	{
		Util.DrawActualSizeTexture(PayWithMobilePos, PayWithMobileTexture);
		GUI.Label(PayByMobileTextRect,"Pay By Mobile",MenuSkin.GetStyle("price_item"));
		return GUI.Button(PayByMobileRect,"",MenuSkin.GetStyle("invisible_button"));
	}

	//  JSON Deserializing code // 

	// try/catch  structure has been chosen over hasKey/typechecking structure to promote readability

	bool DeserializeUserCurrency(string text, out string currency, out double userCurrencyUSDExchangeInverse)
	{
		try 
		{
		  	var data = Json.Deserialize(text) as Dictionary<string,object>;
		  	var currencyData = (Dictionary<string,object>)data["currency"];
		  	currency = (string)currencyData["user_currency"];
		  	userCurrencyUSDExchangeInverse = Convert.ToDouble(currencyData["usd_exchange_inverse"]);
			return true;
		}
		catch(System.InvalidCastException e){Util.Log ("InvalidCastException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.Collections.Generic.KeyNotFoundException e){Util.Log ("KeyNotFoundException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.NullReferenceException e){Util.Log ("NullReferenceException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    currency = "";
	    userCurrencyUSDExchangeInverse = 1.0;
	    return false;
	}

	bool DeserializeCoinPackage(string text, ref CoinPackage coinPackage) 
	{
		try
    	{
    		var data = Json.Deserialize(text) as Dictionary<string,object>;
			coinPackage.url = (string)data["url"];
			List<object> prices = (List<object>)(((Dictionary<string,object>)(data["data"]))["price"]);
			foreach (object priceObj in prices)
			{
				var price = (Dictionary<string,object>)priceObj;
				coinPackage.price[(string)price["currency"]]  = (double)(double)price["amount"];
    		}
    		coinPackage.valid = true;
    		Util.Log("coinPackage" + Convert.ToString(coinPackage.valid));
    		return true;
	    }
	    catch(System.InvalidCastException e){Util.Log ("InvalidCastException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.Collections.Generic.KeyNotFoundException e){Util.Log ("KeyNotFoundException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.NullReferenceException e){Util.Log ("NullReferenceException"); Util.Log (e.Message);Util.Log (e.StackTrace);}

	    return false;
	}

	bool DeserializeMobilePricePoints(string text, List<MobilePricePoint> mobilePricePoints)
	{
		try 
		{
			var data = Json.Deserialize(text) as Dictionary<string,object>;
			List<object> pricePointsList = (List<object>)(((Dictionary<string,object>)data["payment_mobile_pricepoints"])["pricepoints"]);
			
			foreach (var pricePointObj in pricePointsList)
			{
				MobilePricePoint mobilePricePoint;
				var pricePoint = (Dictionary<string,object>)pricePointObj;
				mobilePricePoint.id = (string) pricePoint["pricepoint_id"];
				mobilePricePoint.payerAmount = Convert.ToDouble((string) pricePoint["payer_amount"]);
				mobilePricePoint.payoutAmount = Convert.ToDouble((string) pricePoint["payout_base_amount"]);
				mobilePricePoint.currency = (string) pricePoint["currency"];
				mobilePricePoint.country = (string) pricePoint["country"];

				mobilePricePoints.Add(mobilePricePoint);
			}
			return true;
		}
		catch(System.InvalidCastException e){Util.Log ("InvalidCastException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.Collections.Generic.KeyNotFoundException e){Util.Log ("KeyNotFoundException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.NullReferenceException e){Util.Log ("NullReferenceException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    return false;
	}

	bool DeserializePaymentResponse (string text, out string paymentID){
		try 
		{
		  	var data = Json.Deserialize(text) as Dictionary<string,object>;
		  	paymentID = Convert.ToString(data["payment_id"]);
			return true;
		}
		catch(System.InvalidCastException e){Util.Log ("InvalidCastException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.Collections.Generic.KeyNotFoundException e){Util.Log ("KeyNotFoundException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    catch(System.NullReferenceException e){Util.Log ("NullReferenceException"); Util.Log (e.Message);Util.Log (e.StackTrace);}
	    paymentID = "";
	    return false;
	}
}

