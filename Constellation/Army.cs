﻿using System;

using System.Drawing;

namespace Constellation
{
    public class Army
    {
        public Army(int num, FactoryNode start, Player owner, FactoryNode end, Road road)
        {
            this.num = num; 
            this.owner = owner; 
            this.target = end.loc;
            this.road = road; 
            loc_true = start.loc;
            this.angle = (float)UTILS.Angle(end.loc, start.loc);
            
            //players and roads need end know of new army creation end add it end their lists
			this.owner.armies.Add(this);
			road.armies.Add(this);
        }
        
        public int num; 
        public Road road;
		public Point loc { get { return new Point((int)Math.Floor(loc_true.X), (int)Math.Floor(loc_true.Y)); } }
        public PointF loc_true; 
        public Player owner; 
        public Point target;
        public float angle;
        public float speed;
		public void Move()
		{
			//-------road type determines MOVEMENT SPEED=============  
			speed = road.travelSpeed;

			loc_true.X += speed * (float)Math.Cos(angle);
			loc_true.Y += speed * (float)Math.Sin(angle);
		}
		public int Fight(int b)
		{
			//returns ENEMY left, and automatically updates myself
			/*
             * ================= KEEP THIS: very interesting combat model
            while (Math.Min(this.num, b) > 0)
            {
                //subtracts at least one
                //law of attrition
                int c = this.num;
                //my loss
                this.num -= Math.Max(1, (int)Math.Floor(.5 * b));
                //count dead
                this.owner.dead+=Math.Max(1, (int)Math.Floor(.5 * b));


                b -= Math.Max(1, (int)Math.Floor(.5 * c));
                //enemy's dead count in another place
            }
            */

			//when you fight, things DIE
			//lancaster linear model (https://docs.google.com/file/d/0B4b33Mj-Zk1WNkplamlLOTRHZUE/edit) , also in folder)
			//3 cases
			if (b == this.num) {
				this.owner.dead += this.num;
				this.num = 0;
				b = 0;
			} else if (b > this.num) {
				b -= this.num * this.num / b;
				this.owner.dead += this.num;
				this.num = 0;
			} else if (this.num > b) {
				this.num -= b * b / this.num;
				this.owner.dead += b * b / this.num;
				b = 0;
			}
			return b;
		}
        public bool shouldRemove = false;
        public void CollideEnemy(Army a)
        {

            int meLeft = a.Fight(num);
            if (meLeft <= 0)
            {
                shouldRemove = true;
            }
            else
                num = meLeft;

        }
		public int radius {
			get
			{
				if (num <= 1)
					return 9;
				else
					return 9 + (int)Math.Floor(Math.Log10(num)) * 4;
			}
		}
        

    }
}