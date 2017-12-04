
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ClarionSimulation;
using WorldServerLibrary;
using WorldServerLibrary.Model;
using WorldServerLibrary.Exceptions;
using System.IO;
using System.Web.Script.Serialization;

namespace ClarionDEMO
{
	class MainClass
	{
		#region properties

		public Thread thingsInWorld;
		private WorldServer worldServer = null;
		private ClarionAgent agent;
		String creatureId = String.Empty;
		String creatureName = String.Empty;

		int width = 800;
		int height = 600;
		int counter = 0;
		int time = 0;
		int defaultTime = 10;

		List<Result> creatureEnergySpent;
		List<Result> creatureScore;

		StreamWriter fileEnergySpent;
		StreamWriter fileCreatureScore;

		#endregion

		#region constructor
		public MainClass() {

			Console.WriteLine ("Clarion Demo V0.5");
			agent = new ClarionAgent();
			agent.OnNewVisualSensorialInformation += new InputVisualSensorialInformationEventHandler(agent_OnNewVisualSensorialInformation);
			agent.OnNewExternalActionSelected += new OutputActionChunkEventHandler(agent_OnNewExternalActionSelected);

			fileCreatureScore = File.CreateText ("./reportFiles/Clarion_Score" + DateTime.Now.ToString ("yyyyMMdd_HHmmss") + ".txt");
			fileEnergySpent = File.CreateText ("./reportFiles/Clarion_EnergySpent" + DateTime.Now.ToString ("yyyyMMdd_HHmmss") + ".txt");

			creatureEnergySpent = new List<Result> ();
			creatureScore = new List<Result> ();

			try
			{
				worldServer = new WorldServer("localhost", 4011);
				String message = worldServer.Connect();

				if (worldServer != null && worldServer.IsConnected)
				{
					Console.Out.WriteLine ("[SUCCESS] " + message + "\n");

					worldServer.SendWorldReset();
					worldServer.NewCreature(100, 450, 0, out creatureId, out creatureName);
					worldServer.SendCreateLeaflet();
					generateJewels(9);
					generateFoods(3);
					generateBricks(3);
					generateDeliverySpot();

					if (!String.IsNullOrWhiteSpace(creatureId))
					{
						worldServer.SendStartCamera(creatureId);
					}

					Console.Out.WriteLine("Creature created with name: " + creatureId + "\n");
					agent.Run();
					worldServer.SendStartCreature(creatureId);

					Console.Out.WriteLine("Running Simulation ...\n");

					thingsInWorld = new Thread(Run);
					thingsInWorld.Start();
				}
			}
			catch (WorldServerInvalidArgument invalidArtgument)
			{
				thingsInWorld.Abort ();
				Console.Out.WriteLine(String.Format("[ERROR] Invalid Argument: {0}\n", invalidArtgument.Message));
			}
			catch (WorldServerConnectionError serverError)
			{
				thingsInWorld.Abort ();
				Console.Out.WriteLine(String.Format("[ERROR] Is is not possible to connect to server: {0}\n", serverError.Message));
			}
			catch (Exception ex)
			{
				thingsInWorld.Abort ();
				Console.Out.WriteLine(String.Format("[ERROR] Unknown Error: {0}\n", ex.Message));
			}
		}
		#endregion

		#region Methods
		public static void Main (string[] args)	{
			new MainClass();
		}

		private void Run(object obj){

			bool flag = true;

			while (flag) {
				
				if ((counter /60) == 1) {
					generateFoods(1);
					generateJewels(3);
					counter = 0;
				}

				reportEnergySpent(time);
				reportCreatureScore(time);

				if ((time / 60) == defaultTime) {

					finalizeReport("Creature's Energy", "Time", "Energy", creatureEnergySpent, fileEnergySpent);
					finalizeReport("Creature's Score", "Time", "Score", creatureScore, fileCreatureScore);
					agent.Abort (true);
					flag = false;

				}
					
				counter++;
				time++;
			
				Thread.Sleep (1000);
			}


			System.Environment.Exit(0);
		}

		private void reportCreatureScore(double time) {
			this.creatureScore.Add(new Result("Score Obtained", time, agent.score));
		}

		private void reportEnergySpent(double time) {
			this.creatureEnergySpent.Add(new Result("Energy Spent", time, agent.fuel));
		}


		private void finalizeReport(String graphName, String xTitle, String yTitle, List<Result> results, StreamWriter file) {
			var json = new JavaScriptSerializer().Serialize(new Graph(graphName, xTitle, yTitle, results));
			file.WriteLine (json);
			file.Close ();
		}


		private void generateDeliverySpot(){
			Random random = new Random ();

			int posX = random.Next (0, width);
			int posY = random.Next (0, height);
			worldServer.sendNewDeliverySpot (4, posX, posY); 
		}

		private void generateBricks(int count){
			Random random = new Random ();

			for (int i=0; i < count; i++) {
				int posX = random.Next (0, width);
				int posY = random.Next (0, height);
				worldServer.NewBrick(4, posX, posY, posX + 20, posY + 20);
			}
		}

		private void generateJewels(int count){
			Random random = new Random ();
		
			for (int i=0; i < count; i++) {
				int posX = random.Next (0, width);
				int posY = random.Next (0, height);
				worldServer.NewJewel (random.Next (0, 6), posX, posY);
			}

		}

		private void generateFoods(int count){
			Random random = new Random ();
		
			for (int i=0; i < count; i++) {
				int posX = random.Next (0, width);
				int posY = random.Next (0, height);
				worldServer.NewFood(random.Next(0,2), posX, posY);
			}

		}

		IList<Thing> agent_OnNewVisualSensorialInformation()
		{
			IList<Thing> response = null;

			if (worldServer != null && worldServer.IsConnected)
			{
				response = worldServer.SendGetCreatureState(creatureName);
				Sack s = worldServer.SendGetSack("0");
				this.agent.sack = s;
				s.print();
			}

			return response;
		}

		void agent_OnNewExternalActionSelected(ClarionAgentActionType externalAction)
		{   Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			if (worldServer != null && worldServer.IsConnected)
			{

				switch (externalAction)
				{
					case ClarionAgentActionType.DO_NOTHING:
						agent.Abort (true);
					break;

					case ClarionAgentActionType.ROTATE:
					if (agent.randomMove == 1) {
							worldServer.SendSetAngle (creatureId, -3, -3, 0);
							Thread.Sleep (500);
							worldServer.SendSetAngle(creatureId, 3, -3, 3);
							Thread.Sleep (500);
							worldServer.SendSetAngle (creatureId, 3, 3, 0);
							Thread.Sleep (500);
						} else {
							worldServer.SendSetAngle (creatureId, 3, -3, 3);
						}
					break;

					case ClarionAgentActionType.MOVE_THING:
						worldServer.SendSetGoTo(creatureId, 3d, 3d, agent.getCurrentThing().X1, agent.getCurrentThing().Y1);
					break;

					case ClarionAgentActionType.GET_THING:
					if (agent.getCurrentThing ().CategoryId == Thing.CATEGORY_JEWEL) {
						worldServer.SendSackIt (creatureId, agent.getCurrentThing ().Name);
					} else if (agent.getCurrentThing ().CategoryId == Thing.categoryPFOOD 
						|| agent.getCurrentThing ().CategoryId == Thing.CATEGORY_NPFOOD  
						|| agent.getCurrentThing ().CategoryId == Thing.CATEGORY_FOOD) {

						Thing buriedFood = agent.buriedThings.Where (x => x.Name == agent.getCurrentThing ().Name).FirstOrDefault ();

						if (buriedFood != null) {
							agent.buriedThings.Remove(buriedFood);
							worldServer.sendUnHideIt(creatureId, agent.getCurrentThing ().Name);
							worldServer.SendEatIt(creatureId, agent.getCurrentThing ().Name);
						} else {
							worldServer.SendEatIt(creatureId, agent.getCurrentThing ().Name);
						}

					} else {
						worldServer.sendDeliverLeaflet (creatureId, agent.leafletID);
						worldServer.SendSetAngle (creatureId, -3, -3, 0);
						Thread.Sleep (500);
						worldServer.SendSetAngle(creatureId, 3, -3, 3);
						Thread.Sleep (500);
						worldServer.SendSetAngle (creatureId, 3, 3, 0);
						Thread.Sleep (500);
					}

					break;

					case ClarionAgentActionType.AVOID_COLLISION:
						worldServer.SendSetAngle (creatureId, -3, -3, 0);
						Thread.Sleep (500);
						worldServer.SendSetAngle(creatureId, 3, -3, 3);
						Thread.Sleep (500);
						worldServer.SendSetAngle (creatureId, 3, 3, 0);
						Thread.Sleep (500);
							
					break;

				case ClarionAgentActionType.BURY_THING:
						worldServer.sendHideIt (creatureId, agent.getCurrentThing ().Name);
					break;

					default:
					break;
				}


			}
		}

		#endregion
	}


}
