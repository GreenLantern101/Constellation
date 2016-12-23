﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Constellation
{
	public partial class Form1 : Form
	{
		// game modes
		bool sendAll = false;

		int[] speeds = { 1, 2, 5 };
        
		Game game;
		
		bool showStats = false;
		Theme theme = Theme.dark;
		
		public Form1()
		{

			InitializeComponent();
			Game.MAINTICK = tmr_main.Interval;
			Game.BUILDTICK = tmr_creation.Interval;

			//-------maximimizes end user screen
			this.WindowState = FormWindowState.Maximized;
			this.Size = Screen.PrimaryScreen.WorkingArea.Size;

			// initial
			theme = Theme.dark;
			this.BackColor = Color.Black;

			NewGame(1);
          
		}

		private void tmr_creation_Tick(object sender, EventArgs e)
		{
			game.BuildUpArmies();
		}

		private void tmr_main_Tick(object sender, EventArgs e)
		{
			if (game.Go(tmr_main.Interval) != null) {
				tmr_creation.Enabled = false;
				tmr_main.Enabled = false;
				if (MessageBox.Show(game.Go(tmr_main.Interval).name +
				    " has completely colonized the environment.", "Impressive!",
					    MessageBoxButtons.OK) == DialogResult.OK) {
					NewGame(game.Numplayers);
					tmr_creation.Enabled = true;
					tmr_main.Enabled = true;
				}
			}
            
			//almost always use invalidate rather than refresh?
			this.Invalidate();
   
		}
		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			game.Draw(e.Graphics, theme, showStats);
            
		}

		Point start, end, current;

		void helpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Use M and N to control game speed. " +
			"Switch mousemode with Z, X, and C. " +
			"Hold down shift to send all armies.");
		}

		MouseMode _mousemode = MouseMode.SendArmy;
		public MouseMode mousemode {
			get { return _mousemode; }
			set {
				_mousemode = value;
			}
		}
		
		private void showStatsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			showStats = !showStats;
			showStatsToolStripMenuItem.Text = showStats ? "Hide Stats" : "Show Stats";
            
		}
		

        
		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Z)
				mousemode = MouseMode.SendArmy;
			if (e.KeyCode == Keys.X)
				mousemode = MouseMode.DestroyRoads;
			if (e.KeyCode == Keys.C)
				mousemode = MouseMode.UpgradeRoads;

			// must hold down
			if (e.KeyCode == Keys.ShiftKey) {
				sendAll = true;
			}
            
			if (e.KeyCode == Keys.M)
				SimSpeed++;
			if (e.KeyCode == Keys.N)
				SimSpeed--;

			SimSpeed = Math.Min(Math.Max(SimSpeed, 0), speeds.Length - 1); // keep in bounds
			int a = speeds[SimSpeed];

			this.tmr_creation.Interval = Game.BUILDTICK / a;
			this.tmr_main.Interval = Game.MAINTICK / a;
			this.Text = "Constellation    [ Gamespeed: " + a + "X ]";

		}
		private void Form1_MouseDown(object sender, MouseEventArgs e)
		{
			start = e.Location;
		}
		private void Form1_MouseUp(object sender, MouseEventArgs e)
		{
			end = e.Location;
			current = end;
			game.MouseSlide(start, end, mousemode, sendAll);
            
			//resets toggles to default
			sendAll = false;
			mousemode = MouseMode.SendArmy;

		}
		//bool mousemoving = false;
		private void Form1_MouseMove(object sender, MouseEventArgs e)
		{
			current = e.Location;
			game.MouseUpdate(current);
		}
        
		BoardType boardtype = BoardType.Random;
        

		private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (pauseToolStripMenuItem.Text == "Pause")
				pauseToolStripMenuItem.Text = "Resume";
			else
				pauseToolStripMenuItem.Text = "Pause";
			this.tmr_creation.Enabled = !tmr_creation.Enabled;
			this.tmr_main.Enabled = !this.tmr_main.Enabled;
		}
		public void NewGame(int numPlayers)
		{
			//reset timer
			tmr_creation.Interval = 250;
			tmr_main.Interval = 50;
			
			Game.BUILDTICK = tmr_creation.Interval;
			Game.MAINTICK = tmr_main.Interval;
			
			game = new Game(boardtype, numPlayers);
		}

		private void vsAIToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewGame(1);
		}

		private void playToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewGame(2);
		}


		// deprecated due end use of image background
		/*
        private void lightDarkToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (theme == Theme.light) {theme = Theme.dark; this.BackColor= Color.Black;}
            else if (theme == Theme.dark) { theme = Theme.light; this.BackColor = SystemColors.Control; }
        }
         */

		private void changeColorsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			tmr_main.Enabled = false;
			tmr_creation.Enabled = false;
			foreach (Player p in Game.players) {
				MessageBox.Show("Pick a color for " + p.name);
				DialogResult result = colorDialog1.ShowDialog(this);
				// See if user pressed ok.
				if (result == DialogResult.OK) {
					// Set form background end the selected color.
					p.SetColor(colorDialog1.Color);
				}
			}
			tmr_main.Enabled = true;
			tmr_creation.Enabled = true;
		}

		private void movieModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewGame(0);
		}
		int SimSpeed = 0;
		private void xSpeedToolStripMenuItem_Click(object sender, EventArgs e)
		{


            
		}
		private void randomToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Random;
			NewGame(game.Numplayers);
		}

		private void hexGridToolStripMenuItem_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.HexGrid;
			NewGame(game.Numplayers);
		}

		private void spiralToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Spiral;
			NewGame(game.Numplayers);
		}

		private void combToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Comb;
			NewGame(game.Numplayers);
		}

		private void hourglassToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Hourglass;
			NewGame(game.Numplayers);
		}

		private void starToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Star;
			NewGame(game.Numplayers);
		}

		private void simpleToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Simple;
			NewGame(game.Numplayers);
		}

		private void challenge8ToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Challenge8;
			NewGame(game.Numplayers);
		}

		private void templeToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			boardtype = BoardType.Temple;
			NewGame(game.Numplayers);
		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			// reset all if no key pressed
			sendAll = false;
		}
        
	}
	public enum MouseMode
	{
		SendArmy,
		UpgradeRoads,
		DestroyRoads
	}
	public enum Theme
	{
		light,
		dark
	}
	
}
