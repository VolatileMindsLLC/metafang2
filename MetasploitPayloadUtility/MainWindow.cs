using System;
using System.Linq;
using Gtk;
using metasploitsharp;
using System.Collections.Generic;

public partial class MainWindow: Gtk.Window
{
	MetasploitSession _session = null;
	MetasploitManager _manager = null;
	VBox _main = null;
	Dictionary<string, object> _payloads = null;
	List<VBox> _dynamicOptions = new List<VBox>();
	List<TreeView> _treeViews = new List<TreeView>();
	Notebook _parentNotebook = null;
	Dictionary<string, Dictionary<string, string>> _newPayloads = new Dictionary<string, Dictionary<string, string>>();


	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{

		this.Resize(600,100);

		_main = new VBox ();

		HBox title = new HBox ();
		title.PackStart (new Label ("Login to your Metasploit RPC instance to begin"), true, true, 0);

		_main.PackStart (title, true, true, 0);

		HBox loginInfo = new HBox ();

		loginInfo.PackStart (new Label ("Host:"), false, false, 20);

		Entry hostEntry = new Entry ();
		loginInfo.PackStart (hostEntry, false, false, 0);

		loginInfo.PackStart (new Label ("User:"), false, false, 20);

		Entry userEntry = new Entry ();
		loginInfo.PackStart (userEntry, false, false, 0);

		loginInfo.PackStart (new Label ("Pass:"), false, false, 20);

		Entry passEntry = new Entry ();
		loginInfo.PackStart (passEntry, false, false, 0);

		_main.PackStart (loginInfo, true, true, 0);

		Button login = new Button ("Login");

		login.Clicked += (object sender, EventArgs e) => {
			try {
				Console.WriteLine("Creating session");
				_session = new MetasploitSession(userEntry.Text, passEntry.Text, hostEntry.Text);
				Console.WriteLine("Creating manager and getting current list of payloads");
				_manager = new MetasploitManager(_session);
				_payloads = _manager.GetPayloads();
				BuildWorkspace();
			}
			catch (Exception ex) {
				Console.WriteLine("oh noes, login failed " + ex.ToString());
			}
		};

		HBox loginBox = new HBox ();
		loginBox.PackStart (login, false, false, 300);

		_main.PackStart (loginBox, true, true, 0);

		_main.ShowAll ();
		this.Add (_main);
	}

	protected void BuildWorkspace()
	{
		this.Remove (_main);
		_main = null;
		this.Resize (800, 600);

		_main = new VBox ();

		_parentNotebook = new Notebook ();

		AddPlatformTab ("Linux x86", "linux/x86", _parentNotebook);
		AddPlatformTab ("Linux x86-64", "linux/x64", _parentNotebook);
		AddPlatformTab ("Windows x86", "windows", _parentNotebook, "x64");
		AddPlatformTab ("Windows x86-64", "windows/x64", _parentNotebook);

		_main.PackStart (_parentNotebook, false, false, 10);

		HBox buttons = new HBox ();

		buttons.PackStart (new CheckButton ("Encrypted") { TooltipText = "Encrypted payloads will be bruteforced at runtime" }, false, false, 0);
		buttons.PackEnd (new Button ("Close"), false, false, 10);
		buttons.PackEnd (new Button ("Generate"), false, false, 10);
		_main.PackStart (buttons, false, false, 0);
		_main.ShowAll ();
		this.Add (_main);
	}

	protected void AddPlatformTab(string friendlyName, string msfPayloadFilter, Notebook parent, string negativeFilter = null, Widget payloadDetails = null)
	{
		HBox split = new HBox ();

		TreeView payloads = new TreeView ();

		TreeViewColumn no = new TreeViewColumn ();
		no.Title = "#";
		CellRendererText noText = new CellRendererText ();
		no.PackStart (noText, true);
		no.AddAttribute (noText, "text", 0);

		TreeViewColumn treedeets = new TreeViewColumn ();
		payloads.AppendColumn (no);

		CellRendererText treeDeetsText = new CellRendererText ();

		treedeets.Title = "Details";
		treedeets.PackStart (treeDeetsText, true);
		treedeets.AddAttribute (treeDeetsText, "text", 1);
		payloads.AppendColumn (treedeets);

		ListStore payloadListStore = new ListStore (typeof(string), typeof(string));

		payloads.Model = payloadListStore;

		payloads.WidthRequest = 250;
		payloads.HeightRequest = 500;

		_treeViews.Add (payloads);

		split.PackStart (payloads, false, false, 10);
		VBox deetsAndButtons = new VBox ();
		VBox deets = new VBox ();


		HBox payloadComboContainer = new HBox ();
		ComboBox payloadCombo = ComboBox.NewText ();
		payloadCombo.Changed += OnPayloadChanged;
		payloadCombo.WidthRequest = 250;

		var ps = ((List<object>)_payloads ["modules"]).Where (s => ((string)s).StartsWith (msfPayloadFilter));

		if (negativeFilter != null)
			ps = ps.Where (s => !((string)s).Contains (negativeFilter));

		foreach (var payload in ps.OrderBy(s => s)) 
			payloadCombo.AppendText (payload.ToString());

		payloadComboContainer.PackStart (payloadCombo, false, false, 0);

		deetsAndButtons.PackStart (payloadComboContainer, false, false, 0);
		deetsAndButtons.PackStart (deets, false, false, 0);

		if (payloadDetails != null)
			deets.PackStart (payloadDetails, false, false, 0);
			
		_dynamicOptions.Add (deets);
		split.PackEnd (deetsAndButtons, true, true, 0);
		parent.AppendPage (split, new Label (friendlyName));
	}

	protected void OnPayloadChanged(object o, EventArgs e) {
		VBox payloadDetails = _dynamicOptions [_parentNotebook.CurrentPage];

		foreach (Widget widget in payloadDetails.Children)
			payloadDetails.Remove (widget);

		ComboBox combo = (ComboBox)o;
		TreeIter iter;

		Dictionary<string, object> opts = null;
		if (combo.GetActiveIter (out iter))
			opts = _manager.GetModuleOptions ("payload", ((ComboBox)o).Model.GetValue (iter, 0).ToString());

		foreach (var opt in opts) {
			string optName = opt.Key as string;
			string type = string.Empty;
			string defolt = string.Empty;
			string required = string.Empty;
			string advanced = string.Empty;
			string evasion = string.Empty;
			string desc = string.Empty;

			foreach (var optarg in opt.Value as Dictionary<string, object>) {

				switch (optarg.Key) {
				case "default":
					defolt = optarg.Value.ToString();
					break;
				case "type":
					type = optarg.Value.ToString ();
					break;
				case "required":
					required = optarg.Value.ToString();
					break;
				case "advanced":
					advanced = optarg.Value.ToString();
					break;
				case "evasion":
					evasion = optarg.Value.ToString();
					break;
				case "desc":
					desc = optarg.Value.ToString();
					break;
				default:
					throw new Exception ("Don't know option argument: " + optarg.Key);
				}
			}

			payloadDetails.PackStart (CreateWidget (optName, type, defolt, desc), false, false, 0);
		}

		HBox addBox = new HBox ();
		Button addPayload = new Button ("Add payload");
		addPayload.Clicked += (object sender, EventArgs es) => {
			Dictionary<string, string> newopts = new Dictionary<string, string>();
			foreach (Widget child in _dynamicOptions[_parentNotebook.CurrentPage].Children) {
				if (child is CheckButton)
					newopts.Add((child as CheckButton).Label, (child as CheckButton).Active.ToString());
				else if (child is Entry)
					newopts.Add((child as Entry).TooltipText, (child as Entry).Text);
			}

			TreeIter i; 
			((ComboBox)o).GetActiveIter(out i);

			_newPayloads.Add(((ComboBox)o).Model.GetValue (i, 0).ToString(), newopts);

			((ListStore)_treeViews[_parentNotebook.CurrentPage].Model).AppendValues("0", ((ComboBox)o).Model.GetValue (iter, 0).ToString());

			CellRendererText tx = new CellRendererText();
			_treeViews[_parentNotebook.CurrentPage].Columns[1].PackStart(tx, true);
			_treeViews[_parentNotebook.CurrentPage].ShowAll();

		};
		addBox.PackStart (addPayload, false, false, 0);
		payloadDetails.PackStart (addBox, false, false, 0);
		payloadDetails.ShowAll ();
	}

	Widget CreateWidget(string optName, string type, string defolt, string desc) 
	{
		if (type == "bool") { 
			CheckButton button = new CheckButton (optName);
			button.TooltipText = desc;
			if (defolt == "True")
				button.Activate ();
			return button;
		} else if (type == "string" || type == "address" || type == "port" || type == "integer" || type == "raw" || type == "path") {
			Entry textbox = new Entry (defolt);
			textbox.WidthRequest = 150;
			textbox.TooltipText = optName;
			HBox box = new HBox ();
			Label optNameLabel = new Label (optName);
			optNameLabel.TooltipText = desc;
			optNameLabel.SetAlignment (0f, 0.5f);
			optNameLabel.WidthRequest = 200;
			box.PackStart (optNameLabel, false, false, 0);
			box.PackStart (textbox, false, false, 10);

			return box;
		} else
			throw new Exception ("WTF IS " + type);
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}
}
