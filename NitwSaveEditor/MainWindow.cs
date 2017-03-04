using System;
using Gtk;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

public partial class MainWindow : Gtk.Window
{
	private System.IO.FileStream playerDat = null;

	private Dictionary<string, float> vars = new Dictionary<string, float>();
	private Dictionary<string, string> stringVars = new Dictionary<string, string>();
	//private Dictionary<string, int> persistentInts = new Dictionary<string, int>();

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();
		notebook2.CurrentPage = 0;

		var nameRenderer = new CellRendererText();
		nameRenderer.Editable = false;
		treeVars.AppendColumn("Variable", nameRenderer, "text", 0);

		var valueRenderer = new CellRendererText();
		valueRenderer.Editable = true;
		valueRenderer.Edited += onRawValueEdited;
		treeVars.AppendColumn("Value", valueRenderer, "text", 1);
	}

	private void onRawValueEdited(object o, Gtk.EditedArgs args)
	{
		Gtk.TreeIter iter;
		treeVars.Model.GetIter(out iter, new Gtk.TreePath(args.Path));
		try
		{
			vars[(string)treeVars.Model.GetValue(iter, 0)] = float.Parse(args.NewText);
		}
		catch
		{
			Console.WriteLine("Incorrect format");
		}

		// FIXME: probably shouldn't be here
		UpdateRawValues();
	}

	protected virtual void onInsertRawClick(object sender, EventArgs e)
	{
		if (txtValueName.Text.Length == 0)
			return;
		
		vars.Add(txtValueName.Text, 0);
		txtValueName.Text = "";

		UpdateRawValues();
	}

	protected virtual void onDeleteRawClick(object sender, EventArgs e)
	{
		TreeModel model;
		TreeIter iter;

		if (treeVars.Selection.GetSelected(out model, out iter))
			vars.Remove((string)model.GetValue(iter, 0));

		// FIXME: Probably not the best way to do that
		UpdateRawValues();
	}

	protected virtual void onOpenPlayerDatClick(object sender, EventArgs e)
	{
		Gtk.FileChooserDialog filechooser =
		new Gtk.FileChooserDialog("Select your player.dat file",
			this,
			FileChooserAction.Open,
			"Cancel", ResponseType.Cancel,
			"Open", ResponseType.Accept);

		if (filechooser.Run() == (int)ResponseType.Accept)
		{
			playerDat = System.IO.File.Open(filechooser.Filename,
												System.IO.FileMode.Open,
												System.IO.FileAccess.ReadWrite,
												System.IO.FileShare.None);

			if (playerDat == null)
				throw new Exception("player.dat is not opened");

			BinaryFormatter binaryFormatter = new BinaryFormatter();
			vars = (Dictionary<string, float>)binaryFormatter.Deserialize(playerDat);
			stringVars = (Dictionary<string, string>)binaryFormatter.Deserialize(playerDat);

			ParsePlayerDat();

			btnSave.Sensitive = true;
		}

		filechooser.Destroy();

	}

	protected virtual void onSavePlayerDatClick(object sender, EventArgs e)
	{
		if (playerDat == null)
			throw new Exception("player.dat is not opened");

		playerDat.SetLength(0);
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		binaryFormatter.Serialize(playerDat, vars);
		binaryFormatter.Serialize(playerDat, stringVars);

		playerDat.Flush();

		//playerDat.Close();
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	protected virtual void onSceneChange(object sender, EventArgs e)
	{
		Gtk.TreeIter currentScene;
		cmbScene.GetActiveIter(out currentScene);
		stringVars["__scene"] = (string)cmbScene.Model.GetValue(currentScene, 0);
	}

	protected virtual void onActDayChange(object sender, EventArgs e)
	{
		vars["act"] = (float)hsAct.Value;
		vars["day"] = (float)hsDay.Value;
		if (chkNight.Active)
			vars["night"] = 1;
		else
			vars["night"] = 0;

		vars["actday"] = (int)hsAct.Value * 100 + (int)hsDay.Value;

		stringVars["__actDay"] = "A" + hsAct.Value.ToString("F0") + "D" + hsDay.Value.ToString("F0");

		UpdateRawValues();
	}

	private void ParsePlayerDat()
	{
		LoadScene();
		LoadDate();
		LoadBandPracticeData();
		LoadQuestProgress();

		UpdateRawValues();
	}

	private void LoadQuestProgress()
	{
		float value;

		// Friendships
		if (vars.TryGetValue("did_bea_friendship_quest", out value))
			chkBeaFriendship.Active = (int)value == 1;

		if (vars.TryGetValue("did_gregg_friendship_quest", out value))
			chkGreggFriendship.Active = (int)value == 1;

		if (vars.TryGetValue("did_mom_friendship_quest", out value))
			chkMomFriendship.Active = (int)value == 1;

		if (vars.TryGetValue("did_germ_friendship_quest", out value))
			chkGermFriendship.Active = (int)value == 1;

		// Investigations
		if (vars.TryGetValue("did_bea_investigation_quest", out value))
			chkBeaGhostHunting.Active = (int)value == 1;

		if (vars.TryGetValue("did_angus_investigation_quest", out value))
			chkAngusGhostHunting.Active = (int)value == 1;

		if (vars.TryGetValue("did_gregg_investigation_quest", out value))
			chkGreggGhostHunting.Active = (int)value == 1;

		// Quests
		if (vars.TryGetValue("did_birdland_quest", out value))
			chkBirdquest.Active = (int)value == 1;

		if (vars.TryGetValue("selmers_poet", out value))
			chkSelmers.Active = (int)value >= 3;

		if (vars.TryGetValue("found_pentagrams", out value))
			chkPentagrams.Active = (int)value >= 3;

		if (vars.TryGetValue("mallard_rats", out value))
			chkMiracleBabies.Active = (int)value == 1;

		if (vars.TryGetValue("did_clock_quest", out value))
			chkTooth.Active = (int)value == 1;

		if (vars.TryGetValue("found_all_dusk_stars", out value))
			chkStargazer.Active = (int)value == 1;
	}

	private void LoadScene()
	{
		var currentSceneName = stringVars["__scene"];

		// Seriously, wtf GTK#?
		Gtk.TreeIter currentScene;
		cmbScene.Model.GetIterFirst(out currentScene);
		do
		{
			if ((string)cmbScene.Model.GetValue(currentScene, 0) == currentSceneName)
				break;
		} while (cmbScene.Model.IterNext(ref currentScene));

		if ((string)cmbScene.Model.GetValue(currentScene, 0) != currentSceneName)
			throw new Exception("Unknown scene in player.dat");

		cmbScene.SetActiveIter(currentScene);
		cmbScene.Sensitive = true;
	}

	private void LoadDate()
	{
		hsAct.Value = vars["act"];
		hsAct.Sensitive = true;

		hsDay.Value = vars["day"];
		hsDay.Sensitive = true;

		chkNight.Active = (int)vars["night"] == 1;
		chkNight.Sensitive = true;
	}

	private void LoadBandPracticeData()
	{
		lblBandPractice.Text = "";

		for (int songId = 0; songId < 3; songId++)
		{
			String song = "no name";
			switch (songId)
			{
				case 0:
					song = "Die Anywhere Else";
					break;
				case 1:
					song = "Weird Autumn";
					break;
				case 2:
					song = "Pumpkin Head Man";
					break;
			}

			String right = "band_practice_" + songId.ToString() + "_right";
			String wrong = "band_practice_" + songId.ToString() + "_wrong";

			try
			{
				lblBandPractice.Text += song + " [ Right: " + vars[right]
					+ " | Wrong: " + vars[wrong] + " ] ";

				if (vars[right] > 90)
					lblBandPractice.Text += "Maestro\n";
				else if (vars[right] < 50)
					lblBandPractice.Text += "Bass Ackwards\n";
				else
					lblBandPractice.Text += "\n";
			}
			catch
			{
				lblBandPractice.Text += "No data for band practice #" + (songId+1).ToString() + "\n";
			}
		}
	}

	private void UpdateRawValues()
	{
		treeVars.Model = new Gtk.ListStore(typeof(string), typeof(float));
		foreach (KeyValuePair<string, float> kvp in vars)
		{
			((ListStore)treeVars.Model).InsertWithValues(0, kvp.Key, kvp.Value);
		}

		treeVars.Sensitive = true;
	}

	private void SavePlayerDat()
	{
		// FIXME: ???
	}
}
