﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Constellation
{
	public class AI : Player
	{
		public int TimeUntilNextMove = 60;
		int buildTickInterval;
		int mainTickInterval;
		
		public enum FactoryOwner { me, enemy, noone, anyone, notMe}
		public enum RoadExistence { yes, no, any}
		
		public AI(string name, Color color, int buildTick, int mainTick, Rectangle gameWorld)
			: base(name, color, gameWorld)
		{
			this.buildTickInterval = buildTick;
			this.mainTickInterval = mainTick;
			base.is_AI = true;
			Random r;
			for (int i = 0; i < 3; i++) {
				r = new Random();
				int c = r.Next(1, 5);
				while (strategySpots.Contains(c))
					c = r.Next(1, 5);
				strategySpots.Add(c);
			}
		}
		List<Player> players;
		List<int> strategySpots = new List<int>();
		List<FactoryNode> allFacs; //all factories in the game
		bool strategy_setup = false;
		List<Road> roads;
		//my armies only

		public void Do(List<Player> players, List<FactoryNode> allFacs, List<Road> roads)
		{
			
			//update game info
			this.players = players;
			this.roads = roads;
			this.allFacs = allFacs;
			
			//one move per second
			TimeUntilNextMove--;
			if (TimeUntilNextMove <= 0) {
				TimeUntilNextMove = 20;
				
				foreach (FactoryNode fac in allFacs) {
					
					if (!strategy_setup) {
						if (TopMostFac.owner == null && strategySpots.Contains(1))
							TryBuildNewRoad(ClosestFactoryTo(TopMostFac, RoadExistence.no,
								FactoryOwner.me), TopMostFac, roads);
						else if (BottomMostFac.owner == null && strategySpots.Contains(2)) {
							TryBuildNewRoad(ClosestFactoryTo(BottomMostFac, RoadExistence.no,
								FactoryOwner.me), BottomMostFac, roads);
						} else if (LeftMostFac.owner == null && strategySpots.Contains(3)) {
							TryBuildNewRoad(ClosestFactoryTo(LeftMostFac, RoadExistence.no,
								FactoryOwner.me), LeftMostFac, roads);
						} else if (RightMostFac.owner == null && strategySpots.Contains(4)) {
							TryBuildNewRoad(ClosestFactoryTo(RightMostFac, RoadExistence.no,
								FactoryOwner.me), RightMostFac, roads);
						} else if (CenterMostFac.owner == null && strategySpots.Contains(5)) {
							TryBuildNewRoad(ClosestFactoryTo(CenterMostFac, RoadExistence.no,
								FactoryOwner.me), CenterMostFac, roads);
						} else
							strategy_setup = true;
						
					}
					

					//my closest factory to "fac" that has no roads between them
					FactoryNode x = ClosestFactoryTo(fac, RoadExistence.no, FactoryOwner.me);
					
					//the closest enemy factory to "fac", roads or not
					FactoryNode y = ClosestFactoryTo(fac, RoadExistence.any, FactoryOwner.enemy);


					//expand && capture neutral allFacs
					if (fac.owner == null && strategy_setup
					    && x != null && y != null) {
						if (NetIncoming(x) <= 0) { //if i'm not under attack
							//make sure that i can capture and hold that neutral fac
							if (RoadBetween(y, fac) != null && x.armyNumHere - roadCost > y.armyNumHere
							    || RoadBetween(x, fac) == null && x.armyNumHere + 6 > y.armyNumHere)
								TryBuildNewRoad(x, fac, roads);
						}
						
						//allow AI to get the 1st move
						if (y.owner.numFactories == 1)
							TryBuildNewRoad(x, fac, roads);
					}
					
					
					//prioritize attack on enemy centers closest to it
					if (fac.owner == this) {
						List<FactoryNode> f_temporary = UTILS.GetClosestList(fac.loc, allFacs);
						foreach (FactoryNode f in f_temporary) {
							if (f.owner == this || f.owner == null) //makes sure target is an enemy factory
								continue; 
							
							int n = ArmiesToSend(fac, f);
							if (n <= 0)
								continue;
							
							if (RoadBetween(fac, f) != null)
								SendArmy(fac, f, n);
							else if (TryBuildNewRoad(fac, f, roads)) {
								SendArmy(fac, f, n);
								
							}
						}
						
						//TODO: make use of ArmiesToHelp() method!!!! for reinforcing
						
						
						Harvest(fac); //harvests from fac
					}
				}
				
				//UPGRADING
				Road r = ShouldUpgrade();
				if (r != null) {
					//NEVER strengthen a road connecting you and enemy
					TryUpgradeRoad(r.endpoints[0], r.endpoints[1], r);
					
				}
				
				//NEVER DESTROY ROADS
				
			}
		}
		
		#region center-, left-, right-, top-, bottom- most allFacs
		
		//---------------------------------------------these are not the absolute closest factory
		//to the certain location strategic point, they have been purposely "fuzzied up"
		public FactoryNode CenterMostFac
		{
			get
			{
				FactoryNode fac = null;
				float n = 1000;
				foreach (FactoryNode f in allFacs)
				{
					if (UTILS.Distance(f.loc, new Point(gameWorld.Width / 2, gameWorld.Height / 2)) -100<= n)
					{
						n = UTILS.Distance(f.loc, new Point(gameWorld.Width / 2, gameWorld.Height / 2));
						fac = f;
					}
				}
				return fac;
			}
		}
		public FactoryNode LeftMostFac {
			get
			{
				Point p = new Point(0, gameWorld.Height / 2);
				return UTILS.GetClosest(p, allFacs);
			}
		}
		public FactoryNode RightMostFac {
			get
			{
				Point p = new Point(gameWorld.Width, gameWorld.Height / 2);
				return UTILS.GetClosest(p, allFacs);
			}
		}
		public FactoryNode TopMostFac {
			get
			{
				Point p = new Point(gameWorld.Width / 2, 0);
				FactoryNode f = UTILS.GetClosest(p, allFacs);
				return f;
			}
		}
		public FactoryNode BottomMostFac {
			get
			{
				Point p = new Point(gameWorld.Width / 2, gameWorld.Height);
				return UTILS.GetClosest(p, allFacs);
			}
		}
		
		#endregion


		/// <summary>
		/// sends N armies from start to end
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="num"></param>
		public void SendArmy(FactoryNode start, FactoryNode end, int num)
		{	
			Road r = RoadBetween(start, end);
			
			if (r == null 
			    || start.armyNumHere < 50 //don't send small armies
			    || start == end)
				return;
			
			if (num <= 3)
				for (int i = 0; i < num; i++) {
					start.SplitHalf(end, r);
				}
			else
				start.SendAll(end, r);
		}
		
		/// <summary>
		/// calculates how much help to send
		/// </summary>
		/// <param name="source"></param>
		/// <param name="sink"></param>
		/// <returns>number of armies to send (1/2, 1/4, 1/8, etc)</returns>
		public int ArmiesToHelp(FactoryNode source, FactoryNode sink)
		{
			int NEED = NetIncoming(sink) - sink.armyNumHere;

			//can only reinforce other team members,
			if (source.owner == this
			    && sink.owner == this
			    && source != sink
			    //minimum =(19+1)/2=10 army size reinforcement
			    && source.armyNumHere > 19

			    //don't reinforce things too far away
			    && NewUnits(source, sink) <= 40)
			{

				bool danger = false;
				foreach (FactoryNode f in source.factoriesConnected)
				{
					//check if i'm in danger --> self-preservation first
					if (f.owner != this && UTILS.DistSquared(f.loc, source.loc) <
					    UTILS.DistSquared(source.loc, sink.loc)
					    && 7 * f.armyNumHere / 8 + NetIncoming(source) > source.armyNumHere)
						
						danger = true;
				}

				//if need to send reinforcement ==============FOR COMBAT
				if (!danger)
				{
					//prioritize combat before harvesting!!
					if (NEED > 0)
					{
						if (source.armyNumHere / 2 > NEED)
							return 2;
						else if (3 * source.armyNumHere / 4 > NEED)
							return 3;
						else if (7 * source.armyNumHere / 8 > NEED)
							return 4;
						else return 0;
					}

					float sourceEnemies = 0;
					float sinkEnemies = 0;
					foreach (FactoryNode f in source.factoriesConnected)
					{
						//calculate power of possible attack
						float a = NewUnits(f, source);
						if (f.owner != this) sourceEnemies += f.armyNumHere - a;
					}
					foreach (FactoryNode f in sink.factoriesConnected)
					{
						//calculate power of possible attack
						float a = NewUnits(f, source);
						if (f.owner != this) sinkEnemies += f.armyNumHere - a;
					}

					if (sinkEnemies > sourceEnemies)
					{
						//prevention of enemy attack by coagulating at possible intersects of attack
						return 2;

					}
					
				}
			}
			return 0;
		}
		
		/// <summary>
		/// mobilizes forces to the "front lines"
		/// </summary>
		/// <param name="me"></param>
		public void Harvest(FactoryNode me)
		{
			
			//closest connected friendly factory
			FactoryNode friend = ClosestFactoryTo(me, RoadExistence.yes, FactoryOwner.me);
			if (friend == null)
				return;
			
			FactoryNode enemy;
			enemy = ClosestFactoryTo(me, RoadExistence.yes, FactoryOwner.enemy);
			
			if (enemy == null) //only harvest if i'm NOT connected to enemy
				enemy = ClosestFactoryTo(me, RoadExistence.no, FactoryOwner.enemy);
			if (enemy == null)
				return;
				
			//if friend connected to enemy && i'm not in great danger
			if (ClosestFactoryTo(friend, RoadExistence.yes, FactoryOwner.enemy) != null) {
				if (UTILS.DistSquared(enemy.loc, me.loc) > UTILS.DistSquared(friend.loc, me.loc))
					SendArmy(me, friend, 4); //sendAll
			}
				// also if friend not connected
				else {
				//closest unconnected enemy to friend
				FactoryNode a = ClosestFactoryTo(friend, RoadExistence.no, FactoryOwner.enemy);
					
				//if i am farther from the front lines (with a little tolerance), harvest
				if (UTILS.DistSquared(enemy.loc, me.loc) + 40 * 40 > UTILS.DistSquared(friend.loc, a.loc)) {
					SendArmy(me, friend, 4); //sendAll
				}
			}
				
		}
		public int NetIncoming(FactoryNode f)
		{
			//returns net incoming armies, friendlies & enemies balance out
			int attacker_pop = 0;
			foreach (Road r in f.roadsConnected) {
				foreach (Army a in r.armies) {
					if (a.target == f.loc) {
						if (a.owner == this)
							attacker_pop += a.num;
						else
							attacker_pop -= a.num;
					}
				}
			}
			return attacker_pop;
		}
		

		public Road ShouldUpgrade()
		{
			//list of all nodes with >= 3 connections
			foreach (FactoryNode f in this.factoriesOwned)
			{
				if (f.roadsConnected.Count >= 3
				    && f.owner == this
				    && NetIncoming(f) <= f.armyNumHere - roadCost)
				{
					foreach (FactoryNode f2 in f.factoriesConnected)
					{
						//upgrade proportionally end roads leading to each end factory
						//only upgrade if not under attack
						if (f2.roadsConnected.Count >= 3
						    && f2.owner == this
						    && (int)RoadBetween(f, f2).rdtype <
						    (f.roadsConnected.Count+f2.roadsConnected.Count)/3 -1)

							return RoadBetween(f, f2);
					}
				}
			}
			return null;
		}
		/// <summary>
		/// should i attack?
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="toAttack"></param>
		/// <returns></returns>
		public int ArmiesToSend(FactoryNode attacker, FactoryNode toAttack)
		{

			if (attacker.owner == this
			    && toAttack.owner != this
			    && NetIncoming(attacker) <= 0 //make sure I'm not being attacked
			    && NewUnits(attacker, toAttack) <= 80) //don't attack too far things
			{
				int reinforcement_possible= 0;

				foreach (FactoryNode f in toAttack.factoriesConnected)
				{
					//if reinforcement can come on time
					if(f.owner!=this && NumTravelTicks(f, toAttack)<=NumTravelTicks(attacker, toAttack))
						reinforcement_possible += f.armyNumHere / 4;
				}
				
				
				int road_expense = 0;
				//take into account cost of building a new road
				if (RoadBetween(attacker, toAttack) == null) road_expense = roadCost;

				//enemy strength sum
				float N = toAttack.armyNumHere + 
					NewUnits(attacker, toAttack) + 
					reinforcement_possible + 
					road_expense +
					NetIncoming(toAttack);

				//if already being attacked successfully
				if (N < 0 ) return 0;
				else
				{
					//only attack if i am nearly sure to win
					if (.75 * attacker.armyNumHere > N)
						return 4; //sendAll
				}
			}
			return 0;
		}
		
		public float NumTravelTicks(FactoryNode start, FactoryNode end)
		{
			//so if used in reinforcement senses, no reinforcements will come
			if (RoadBetween(start, end) != null)
				return UTILS.Distance(start.loc, end.loc) / RoadBetween(start, end).travelSpeed;
			else
			{
				//imagine road was built, calculate odds --> will build later if good choice?
				Road r = new Road(start, end, RoadTypes.Dirt);
				return UTILS.Distance(start.loc, end.loc) / r.travelSpeed;
			}
		}
		/// <summary>
		/// calculates how many new units will be produced by the time help arrives
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public float NewUnits(FactoryNode start, FactoryNode end)
		{
			return NumTravelTicks(start, end) * mainTickInterval / buildTickInterval;
		}
		
		/// <summary>
		/// returns the road between the two allFacs, if any
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public Road RoadBetween(FactoryNode start, FactoryNode end)
		{
			foreach (Road r in start.roadsConnected) {
				if (r.endpoints.Contains(end))
					return r;
			}
			return null;
		}
		
		public FactoryNode ClosestFactoryTo(FactoryNode fromThis, RoadExistence mustHaveRoad, FactoryOwner f_owner)
		{
			
			List<FactoryNode> f_temporary = UTILS.GetClosestList(fromThis.loc, allFacs);
			foreach (FactoryNode f in f_temporary)
			{
				if (f == fromThis) //closest fac can't be itself!
					continue;
				if (mustHaveRoad==RoadExistence.yes && RoadBetween(fromThis, f) != null
				    || mustHaveRoad==RoadExistence.no && RoadBetween(fromThis,f)==null
				    || mustHaveRoad==RoadExistence.any)
				{
					if (f_owner == FactoryOwner.me && f.owner == this ||
					    f_owner == FactoryOwner.enemy && f.owner != this && f.owner != null ||
					    f_owner == FactoryOwner.noone && f.owner == null ||
					    f_owner == FactoryOwner.anyone ||
					    f_owner == FactoryOwner.notMe && f.owner != this) {
						
						return f;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// finds the closest factory of a certain owner or group of owners from ANY one of
		/// my own allFacs
		/// </summary>
		/// <param name="f_owner"></param>
		/// <param name="mustHaveRoad"></param>
		/// <returns></returns>
		public FactoryNode ClosestFactoryOf(FactoryOwner f_owner, RoadExistence mustHaveRoad)
		{
			float d = 2000*2000; FactoryNode closestFac=null;

			foreach (FactoryNode myFac in this.factoriesOwned) {
				foreach (FactoryNode f in allFacs) {
					if (mustHaveRoad == RoadExistence.yes && RoadBetween(myFac, f) != null
					    || mustHaveRoad == RoadExistence.no && RoadBetween(myFac, f) == null
					    || mustHaveRoad == RoadExistence.any) {
						
						if (f_owner == FactoryOwner.me && f.owner == this ||
						    f_owner == FactoryOwner.enemy && f.owner != this && f.owner != null ||
						    f_owner == FactoryOwner.noone && f.owner == null ||
						    f_owner == FactoryOwner.anyone ||
						    f_owner == FactoryOwner.notMe && f.owner != this) {
							
							if (UTILS.DistSquared(f.loc, myFac.loc) <= d) {
								d = UTILS.DistSquared(f.loc, myFac.loc);
								closestFac = f;
							}
						}
					}
				}
				
			}
			return closestFac;
		}
		
		
		
	}
}