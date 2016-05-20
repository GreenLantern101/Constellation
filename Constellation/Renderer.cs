﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Constellation
{
    public class Renderer
    {
        Theme theme; 
        Game game; 
        bool showStats;
		Graphics g;
		Color c {
			get
			{
				if (theme == Theme.dark)
					return Color.White;
				else
					return Color.Black;
			}
		}
		List<Player> players = new List<Player>();

        String fontStyle = "Microsoft Sans Serif";

        public Renderer(Game game, Theme theme, bool showStats = false)
        {
            this.theme = theme; this.game = game; this.showStats = showStats;
            this.players = game.players;
        }
        public void DrawParticles(ParticleEmitter partEmit)
        {
            //draws all particles start particleEmitters
            foreach (Particle p in partEmit.particles)
            {
                var col = Color.FromArgb(Math.Max(255 - Convert.ToInt32(p.age / p.lifetime * 255), 0), p.color);

                //random size particles
                Brush b = new SolidBrush(col);
                g.FillEllipse(b, Convert.ToSingle(p.loc.X - 1),
                    Convert.ToSingle(p.loc.Y - 1), p.radius, p.radius);
            }

        }
        public void UpdateInfo(bool showStats, Theme theme)
        {
            this.showStats = showStats; this.theme = theme;
        }
		public void Render(Graphics g)
		{
			this.g = g;
			//draws EVERYTHING

			//===================== temp vars
			int gameXright = game.gameWorld.Width + game.gameWorld.X;
			//=====================


			foreach (ParticleEmitter pe in game.particleEmitters) {
				DrawParticles(pe);
			}

			//if (mouseOverThisFactory != null)
			//{
			//    var font = new Font(fontStyle, 28F);
			//    g.DrawString(mouseOverThisFactory.armyNumHere.ToString(), font,
			//        new SolidBrush(mouseOverThisFactory.owner.color),
			//        new Point(mouseOverThisFactory.loc.X - 10,
			//            mouseOverThisFactory.loc.Y - 34 - mouseOverThisFactory.radius));
			//}

			if (showStats) {
				//Brush b = new SolidBrush(Color.FromArgb(128, p.color));
				
				foreach (Player p in game.players) {
                    Brush b = new SolidBrush(p.color);

					using (Font font = new Font(fontStyle, 16F)) {
						g.DrawString(p.numFactories + " *", font, b,
							new Point(gameXright + 20, 120 * players.IndexOf(p) + 30));
						//total strength------------
						int strength = 0;
						foreach (FactoryNode f in p.factoriesOwned)
							strength += f.armyNumHere;
						foreach (Army a in p.armies)
							strength += a.num;

						g.DrawString("+ " + strength, new Font(fontStyle, 20F), b,
							new Point(gameXright + 20, 120 * players.IndexOf(p) + 60));
						g.DrawString("- " + p.dead, new Font(fontStyle, 12F), b,
							new Point(gameXright + 20, 120 * players.IndexOf(p) + 90));
					}
				}

				//total time elapsed
				int seconds = (int)Math.Floor((decimal)game.totalTime / 1000);
				int minutes = (int)Math.Floor((decimal)seconds / 60);
				seconds -= 60 * minutes;
				string space1 = "";
				string space2 = "";
				if (minutes < 10)
					space1 = "0";
				if (seconds < 10)
					space2 = "0";
				g.DrawString(space1 + minutes + " : " + space2 + seconds, new Font(fontStyle, 22F), new SolidBrush(c),
					new Point(gameXright + 20, players.Count * 120 + 60));

			}
			//---------------------------------------------this has a layering effect...
			foreach (Player p in players) {
				foreach (Army a in p.armies) {				
					Draw(a);
				}
			}
			foreach (FactoryNode f in game.factorynodes)
				Draw(f);
			if (game.target != null)
				DrawTarget(game.target); // draw where the fleet will be sent
			foreach (Road r in game.roads)
				Draw(r);

		}
        public void DrawTarget(FactoryNode facNode)
        {
            int radius = facNode.radius + 10;
            Point loc = facNode.loc;
            Color c;
            if (facNode.owner==null || facNode.owner.color !=Color.Lime) c = Color.Lime;
            else c = Color.Cyan;
            g.DrawEllipse(new Pen(new SolidBrush(c),3), loc.X - facNode.anim, loc.Y - facNode.anim,
                    2 * facNode.anim, 2 * facNode.anim);
        }
        public void Draw(FactoryNode facNode)
        {
            //draw factory nodes

            //===================== temp vars
            int radius = facNode.radius;
            Point loc = facNode.loc;
            
            //=====================

            if (facNode.owner == null)
            {
                //can't use temp var for anim or direction, because anim must UPDATE!!!
                if (facNode.anim <= 2) facNode.direction = 1;
                else if (facNode.anim >= radius) facNode.direction = -1;
                facNode.anim += facNode.direction * .2f;
                
                
                g.DrawEllipse(new Pen(new SolidBrush(c), 3), loc.X - facNode.anim, loc.Y - facNode.anim,
                    2 * facNode.anim, 2 * facNode.anim);
            }

            else
            {
            	Color color = facNode.owner.color;
                g.DrawEllipse(new Pen(new SolidBrush(color),3), loc.X - radius/2, loc.Y - radius/2,
                    radius, radius);

                g.DrawEllipse(new Pen(new SolidBrush(Color.FromArgb(32, color)), 11),
                    loc.X - radius/2, loc.Y - radius/2,
                    radius, radius);
                 

				//eventually, remove numbers all together!!
				using (Font font = new Font(fontStyle, 15F)) {
					using (StringFormat stringFormat = new StringFormat()) {
						stringFormat.Alignment = StringAlignment.Center;
						RectangleF rect = new RectangleF(loc.X - 100, loc.Y - radius - 25, 200, 25);
						g.DrawString(facNode.armyNumHere.ToString(), font, new SolidBrush(color),
							rect, stringFormat);
					}
				}
            }
        }
		public void Draw(Road road)
		{
			int a = (int)road.rdtype;

			float[] dashValues = { 2 * a + 1, 2 };
			using (Pen p = new Pen(c, a)) {
				p.DashPattern = dashValues;
				

				g.DrawLine(p, road.endpoints[0].loc, road.endpoints[1].loc);
			}
		}
		public void Draw(Army army)
		{

			//===================== temporary vars
			PointF loc_true = army.loc_true;
			int radius = army.radius;
			int num = army.num;
			//=====================

			//largest power end two still smaller than the number
            
			int largestPowerOfTwo;
			
			if (num > 0)
				//Math.log(2) is approx .301
				largestPowerOfTwo = Math.Max(0, (int)Math.Floor(Math.Log(num) / .3f));
			else
				largestPowerOfTwo = 1;
			
			//Color c= Color.FromArgb(Math.Min(25*largestPowerOfTwo,250), army.owner.color);

			Color highlight = Color.FromArgb(Math.Min(25 * largestPowerOfTwo, 250), c);
			



			RectangleF rect = new RectangleF(
				                  loc_true.X - radius, loc_true.Y - radius, 2 * radius, 2 * radius);

			//experimental arrow

			//clockwise
			PointF[] pts = new PointF[4];
			pts[0] = new PointF(loc_true.X - radius, loc_true.Y - radius);
			pts[1] = new PointF(loc_true.X, loc_true.Y + radius);
			pts[2] = new PointF(loc_true.X + radius, loc_true.Y - radius);           
			pts[3] = new PointF(loc_true.X, loc_true.Y - radius / 2);
			//...
			Rotate(pts, loc_true, (float)army.angle);
			g.DrawPolygon(new Pen(new SolidBrush(army.owner.color), 3), pts);

		}


        //
        //=========== helper methods ===============
        //

        //end rotate sets of points
        void Rotate(PointF[] points, PointF center, float degreesCounterClockwise)
        {
            float ang = degreesCounterClockwise;
            
            for (int i = 0; i < points.Count(); i++)
            {
				float halfPi = 1.5708f; //Math.PI/2
				
                float ang_orig = (float)Math.Atan2(points[i].Y - center.Y, points[i].X - center.X)-halfPi;
                points[i] = new PointF(
                    UTILS.Distance(points[i], center) * (float)Math.Cos(ang+ang_orig) + center.X,
                    UTILS.Distance(points[i], center) * (float)Math.Sin(ang+ang_orig) + center.Y);
            }
        }
        
    }
}