using System;
using System.Collections.Generic;
using System.Linq;
using Clarion;
using Clarion.Framework;
using Clarion.Framework.Core;
using Clarion.Framework.Templates;
using WorldServerLibrary.Model;
using System.Threading;
using Gtk;

namespace ClarionSimulation
{
	/// <summary>

	/// This event will be fired at the beginning of each cognitive cycle in order to get the current visual sensor information.
	/// </summary>
	/// <returns>The Visual Sensor Information Itens</returns> 
	public delegate IList<Thing> InputVisualSensorialInformationEventHandler();

	/// <summary>
	/// This event will be fired at the end of each cognitive cycle in order to translate the selected action to a World Server Command
	/// </summary>
	/// <param name="externalAction">The external action that was choice</param>
	public delegate void OutputActionChunkEventHandler(ClarionAgentActionType externalAction);

	/// <summary>
	/// Public enum that represents all possibilities of agent actions
	/// </summary>
	public enum ClarionAgentActionType
	{
		DO_NOTHING,
		ROTATE,
		MOVE_THING,
		GET_THING,
		AVOID_COLLISION,
		DELIVERY_SPOT,
		RANDOM_MOVE,
		BURY_THING
	}

	public class ClarionAgent
	{
		#region Constants

		/// <summary>
		/// Constant that represents the Visual Sensor
		/// </summary>
		private String SENSOR_VISUAL_DIMENSION = "VisualSensor";

		private String ROTATE_MOVE_THING = "MoveRotateThing";

		private String GET_THING = "GetThing";

		private String BURY_THING = "BuryThing";

		private String NOTHING = "Nothing";

		private String AVOID_COLLISION = "AvoidCollision";

		private Thing targetThing = null;

		public string leafletID = "";

		public List<Thing> buriedThings;

		public DateTime randomDateTime;

		private double previousXpos;

		private double previousYpos;

		private double currentXpos;

		private double currentYpos;

		public int randomMove;

		public Creature pCreature;

		public double score = 0;

		public double fuel = 1000;

		#endregion

		#region Properties

		#region Simulation

		/// <summary>
		/// If this value is greater than zero, the agent will have a finite number of cognitive cycle. Otherwise, it will have infinite cycles.
		/// </summary>
		public double MaxNumberOfCognitiveCycles = -1;

		/// <summary>
		/// Current cognitive cycle number
		/// </summary>
		private double CurrentCognitiveCycle = 0;

		/// <summary>
		/// Time between cognitive cycle in miliseconds
		/// </summary>
		public Int32 TimeBetweenCognitiveCycles = 0;

		/// <summary>
		/// A thread Class that will handle the simulation process
		/// </summary>
		private Thread runThread;


		public Sack sack;

		#endregion

		#region Agent

		/// <summary>
		/// The agent 
		/// </summary>
		private Clarion.Framework.Agent CurrentAgent;

		/// <summary>
		/// For each cognitive cycle, this event will be called in order to the agent receives the current sensorial information
		/// </summary>
		public event InputVisualSensorialInformationEventHandler OnNewVisualSensorialInformation;

		/// <summary>
		/// For each cognitive cycle, this event will be called when the agent selects one action
		/// </summary>
		public event OutputActionChunkEventHandler OnNewExternalActionSelected;

		#endregion

		#region Perception Input

		private DimensionValuePair InputNothing;

		private DimensionValuePair InputGetThing;

		private DimensionValuePair InputMoveRotateThing;

		private DimensionValuePair InputAvoid;

		private DimensionValuePair InputBuryThing;
	
		                          
		#endregion

		#region Action Output

		private ExternalActionChunk OutputNothing;

		private ExternalActionChunk OutputRotateClockwise;

		private ExternalActionChunk OutputMoveThing;

		private ExternalActionChunk OutputGetThing;

		private ExternalActionChunk OutputAvoidCollision;

		private ExternalActionChunk OutputBuryThing;

		#endregion

		#endregion

		#region Constructor

		public ClarionAgent()
		{
			
			// Initialize the agent
			CurrentAgent = World.NewAgent("Current Agent");

			//Application.Init ();
			//Mind m = new Mind();
			//m.Show ();
			//Application.Run ();

			buriedThings = new List<Thing> ();

			// Initialize Input Information
			InputNothing = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, NOTHING);
			InputMoveRotateThing = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, ROTATE_MOVE_THING);
			InputGetThing = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, GET_THING);
			InputAvoid = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, AVOID_COLLISION);
			InputBuryThing = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, BURY_THING);

			// Initialize Output actions
			OutputRotateClockwise = World.NewExternalActionChunk(ClarionAgentActionType.ROTATE.ToString());
			OutputMoveThing = World.NewExternalActionChunk (ClarionAgentActionType.MOVE_THING.ToString());
			OutputGetThing = World.NewExternalActionChunk (ClarionAgentActionType.GET_THING.ToString());
			OutputNothing = World.NewExternalActionChunk(ClarionAgentActionType.DO_NOTHING.ToString());
			OutputAvoidCollision = World.NewExternalActionChunk(ClarionAgentActionType.AVOID_COLLISION.ToString());
			OutputBuryThing = World.NewExternalActionChunk(ClarionAgentActionType.BURY_THING.ToString());
		

			randomDateTime = DateTime.Now;
			currentXpos = previousXpos = 100;
			currentYpos = previousYpos = 450;
			randomMove = 0;

			//Create thread to simulation
			runThread = new Thread(RunThread);
			Console.WriteLine("Agent started");
		}

		#endregion

		#region Public Methods

		public Thing getCurrentThing(){
			return targetThing;
		}

		/// <summary>
		/// Run the Simulation in World Server 3d Environment
		/// </summary>
		public void Run()
		{                
			Console.WriteLine ("Running ...");
			// Setup Agent to run
			if (runThread != null && !runThread.IsAlive)
			{
				SetupAgentInfraStructure();

				if (OnNewVisualSensorialInformation != null)
				{
					// Start Simulation Thread                
					runThread.Start(null);
				}
			}
		}

		/// <summary>
		/// Abort the current Simulation
		/// </summary>
		/// <param name="deleteAgent">If true beyond abort the current simulation it will die the agent.</param>
		public void Abort(Boolean deleteAgent)
		{   Console.WriteLine ("Aborting ...");
			if (runThread != null && runThread.IsAlive)
			{
				runThread.Abort();
			}

			if (CurrentAgent != null && deleteAgent)
			{
				CurrentAgent.Die();
			}
		}

		#endregion

		#region Setup Agent Methods

		/// <summary>
		/// Setup agent infra structure (ACS, NACS, MS and MCS)
		/// </summary>
		private void SetupAgentInfraStructure()
		{
			// Setup the ACS Subsystem
			SetupACS();                    
		}

		private void SetupMS()
		{            
			//RichDrive
		}

		/// <summary>
		/// Setup the ACS subsystem
		/// </summary>
		private void SetupACS()
		{
			// Create Colission Rotate Rule
			SupportCalculator avoidRotateSupportCalculator = FixedRuleDelegateToDelegateRotate;
			FixedRule ruleRotate = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, OutputRotateClockwise, avoidRotateSupportCalculator);

			// Commit this rule to Agent (in the ACS)
			CurrentAgent.Commit(ruleRotate);

			SupportCalculator moveThingSupportCalculator = FixedRuleDelegateToMoveToThing;
			FixedRule ruleMoveThing = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, OutputMoveThing, moveThingSupportCalculator);

			// Commit this rule to Agent (in the ACS)
			CurrentAgent.Commit(ruleMoveThing);


			SupportCalculator getThingSupportCalculator = FixedRuleDelegateToGetThing;
			FixedRule ruleGetThing = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, OutputGetThing, getThingSupportCalculator);

			// Commit this rule to Agent (in the ACS)
			CurrentAgent.Commit(ruleGetThing);

			SupportCalculator nothingSupportCalculator = FixedRuleDelegateToNothing;
			FixedRule ruleNothing = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, OutputNothing, nothingSupportCalculator);

			// Commit this rule to Agent (in the ACS)
			CurrentAgent.Commit(ruleNothing);

			// Create Colission Wall Rule
			SupportCalculator avoidCollisionSupportCalculator = FixedRuleDelegateToAvoid;
			FixedRule ruleAvoid = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, OutputAvoidCollision, avoidCollisionSupportCalculator);

			// Commit this rule to Agent (in the ACS)
			CurrentAgent.Commit(ruleAvoid);

			// Create Colission Wall Rule
			SupportCalculator buryThingSupportCalculator = FixedRuleDelegateToBuryThing;
			FixedRule ruleBuryThing = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, OutputBuryThing, buryThingSupportCalculator);

			// Commit this rule to Agent (in the ACS)
			CurrentAgent.Commit(ruleBuryThing);

			// Disable Rule Refinement
			CurrentAgent.ACS.Parameters.PERFORM_RER_REFINEMENT = false;

			// The selection type will be probabilistic
			CurrentAgent.ACS.Parameters.LEVEL_SELECTION_METHOD = ActionCenteredSubsystem.LevelSelectionMethods.STOCHASTIC;

			// The selection of the action will be fixed (not variable) i.e. only the statement defined above.
			CurrentAgent.ACS.Parameters.LEVEL_SELECTION_OPTION = ActionCenteredSubsystem.LevelSelectionOptions.FIXED;

			// Define Probabilistic values
			CurrentAgent.ACS.Parameters.FIXED_FR_LEVEL_SELECTION_MEASURE = 1;
			CurrentAgent.ACS.Parameters.FIXED_IRL_LEVEL_SELECTION_MEASURE = 0;
			CurrentAgent.ACS.Parameters.FIXED_BL_LEVEL_SELECTION_MEASURE = 0;
			CurrentAgent.ACS.Parameters.FIXED_RER_LEVEL_SELECTION_MEASURE = 0;
		}

		/// <summary>
		/// Make the agent perception. In other words, translate the information that came from sensors to a new type that the agent can understand
		/// </summary>
		/// <param name="sensorialInformation">The information that came from server</param>
		/// <returns>The perceived information</returns>
		private SensoryInformation MakePerceptionFromSensorialInput(IList<Thing> sensorialInformation)
		{
			// New sensory information
			SensoryInformation si = World.NewSensoryInformation(CurrentAgent);

			double rotateMoveThingActivationValue = 0;
			double getThingActivationValue = 0;
			double nothingActivationValue = 0;
			double avoidCollisionActivationValue = 0;
			double buryActivationValue = 0;

			bool deliverAction = false;
			bool buryAction = false;

			if (sensorialInformation != null) {

				randomMove = isActiveRandomMovement ();

				//Console.WriteLine(sensorialInformation);
				Creature c = (Creature)sensorialInformation.Where (item => (item.CategoryId == Thing.CATEGORY_CREATURE)).First ();

				fuel = c.Fuel;
				score = c.score;

				currentXpos = c.X1;
				currentYpos = c.Y1;

				List<Thing> listOfJewel = sensorialInformation.Where (item => (item.CategoryId == Thing.CATEGORY_JEWEL)).OrderBy (x => x.DistanceToCreature).ToList ();
				List<Thing> listOfFood = sensorialInformation.Where (item => (item.CategoryId == Thing.CATEGORY_NPFOOD || item.CategoryId == Thing.categoryPFOOD)).OrderBy (x => x.DistanceToCreature).ToList ();
				List<Thing> listOfBricks = sensorialInformation.Where (item => (item.CategoryId == Thing.CATEGORY_BRICK)).OrderBy (x => x.DistanceToCreature).ToList ();

				c.PrintLeaflet ();

				Thing closestJewel = listOfJewel.FirstOrDefault ();
				Thing closestFood = listOfFood.FirstOrDefault ();
				Thing closestBrick = listOfBricks.FirstOrDefault ();
				Thing deliverySpot = sensorialInformation.Where (item => (item.CategoryId == Thing.CATEGORY_DeliverySPOT)).FirstOrDefault ();

				targetThing = null;

				if (closestBrick != null) {
					if (closestBrick.DistanceToCreature <= 35) {
						targetThing = closestBrick;
					} else {
						targetThing = null;
					} 
				} 

				if (targetThing == null && deliverySpot != null) {
					if (deliverySpot.DistanceToCreature <= 50) {
						targetThing = deliverySpot;
					} else {
						targetThing = null;
					} 
				} 


				if (targetThing == null && (c.Fuel / 1000) <= 0.4) {
					if (closestFood != null)
						targetThing = closestFood;
					else {
						if(buriedThings.Count > 0){
							organazingThingList (c, buriedThings);
							targetThing = buriedThings[0];
						}
					}
				}

				if (targetThing == null) {

					leafletID = isCompleted (c);

					if (leafletID != "") {
						if (deliverySpot != null) {
							targetThing = deliverySpot;
							deliverAction = true;
						}
					} 
				}
					
				if (targetThing == null) {
					if (closestJewel != null) {
						if (thereIsJewelInLeaflet (c, closestJewel)) {
							targetThing = closestJewel;
						} 
					}
				}


				if (closestFood != null && closestJewel != null) {
					if (closestFood.DistanceToCreature < closestJewel.DistanceToCreature) {
						if (closestFood.DistanceToCreature < 35) {
							targetThing = closestFood;
							buryAction = true;
							addBuriedFood(closestFood);
						}
					} else {
						if (closestJewel.DistanceToCreature < 35 && !thereIsJewelInLeaflet(c, closestJewel)) {
							targetThing = closestJewel;
							buryAction = true;
						}
					}
				} else {
					if (closestFood != null) {
						if (closestFood.DistanceToCreature < 35) {
							targetThing = closestFood;
							buryAction = true;
							addBuriedFood(closestFood);
						}
					} else if(closestJewel != null) {
						 
						if (closestJewel.DistanceToCreature < 35 && !thereIsJewelInLeaflet(c, closestJewel)) {
							targetThing = closestJewel;
							buryAction = true;
						}
					}
				}



				if (randomMove == 0) {
					if (!buryAction) {
						if (!deliverAction) {
							if (targetThing != null) {
								if (targetThing.CategoryId == Thing.CATEGORY_BRICK || targetThing.CategoryId == Thing.CATEGORY_DeliverySPOT) {
									avoidCollisionActivationValue = 1;

								} else if (targetThing.CategoryId == Thing.CATEGORY_NPFOOD || targetThing.CategoryId == Thing.categoryPFOOD || targetThing.CategoryId == Thing.CATEGORY_JEWEL) {
									if (targetThing.DistanceToCreature >= 35) {
										rotateMoveThingActivationValue = 1;
									} else {
										getThingActivationValue = 1;
									}
								} else {
									rotateMoveThingActivationValue = 0;
								}

							} else {
								rotateMoveThingActivationValue = 0;
							}
						} else {
							if (targetThing.DistanceToCreature >= 80) {
								rotateMoveThingActivationValue = 1;
							} else {
								getThingActivationValue = 1;
							}
						}
					} else {
						buryActivationValue = 1;
					}
				} else {
					rotateMoveThingActivationValue = 0;
				}
			}

			si.Add(InputMoveRotateThing, rotateMoveThingActivationValue);
			si.Add(InputGetThing, getThingActivationValue);
			si.Add(InputNothing, nothingActivationValue);
			si.Add(InputAvoid, avoidCollisionActivationValue);
			si.Add(InputBuryThing, buryActivationValue);

			return si;
		}


		public void organazingThingList(Creature creature, List<Thing> things){

			foreach (Thing thing in things) {
				thing.DistanceToCreature = calculateDistanceToThing (creature, thing);
			}

			things = things.OrderBy (x => x.DistanceToCreature).ToList();
		}

		public double calculateDistanceToThing(Creature creature, Thing thing){

			double distance = Math.Sqrt(Math.Pow((creature.X1 - thing.X1), 2) +
				Math.Pow((creature.Y1 - thing.Y1), 2));

			return distance;
		}

		public int isActiveRandomMovement(){

			DateTime now = DateTime.Now;

			if (currentXpos == previousXpos && currentYpos == previousYpos) {
				
				TimeSpan span = now.Subtract(randomDateTime);
				int seconds = span.Seconds;

				if ((seconds / 20) >= 1) {
					randomDateTime = now;
					return 1;
				} else {
					return 0;
				}
			} else {
				previousXpos = currentXpos;
				previousYpos = currentYpos;
				randomDateTime = now;
				return 0;
			}

		}


		public void addBuriedFood(Thing food){

			if (buriedThings.Count == 0) {
				buriedThings.Add (food);
			} else if (buriedThings.Where(x => x.Name == food.Name).FirstOrDefault() == null) {
				buriedThings.Add (food);
			}

		}
			

		public String isCompleted(Creature c){

			String lId = "";

			foreach (Leaflet leaflet in c.leaflets) {

				if (leaflet.isCompleted()) {
					lId = Convert.ToString(leaflet.leafletID);
					break;
				}

			}

			return lId;

		}

		public bool thereIsJewelInLeaflet(Creature c, Thing jewel){

			List<LeafletItem> listLeafletItem = c.leaflets.SelectMany (x => x.items).ToList ().Where (i => i.itemKey == jewel.Material.Color).ToList ();

			if (listLeafletItem.Count > 0) {
				return true;
			} else {
				return false;
			}

		}

		public bool isCompleted(Creature c, Thing jewel){


				List<LeafletItem> listLeafletItem = c.leaflets.SelectMany (x => x.items).ToList ().Where (i => i.itemKey == jewel.Material.Color).ToList ();

				LeafletItem leafletItem = new LeafletItem ();
				leafletItem.itemKey = jewel.Material.Color;

				foreach (LeafletItem item in listLeafletItem) {
					leafletItem.totalNumber += item.totalNumber;
				}

				return leafletItem.totalNumber == this.getListOfLeafletCollected ().Where (i => i.itemKey == leafletItem.itemKey).FirstOrDefault ().collected ? 
				true : false;
			
		}

		public List<LeafletItem> getListOfLeafletCollected(){

			List<LeafletItem> collected = new List<LeafletItem>();

			collected.Add (new LeafletItem ("Green", 0, sack.green_crystal));
			collected.Add (new LeafletItem ("Red", 0, sack.red_crystal));
			collected.Add (new LeafletItem ("Blue", 0, sack.blue_crystal));
			collected.Add (new LeafletItem ("Yellow", 0, sack.yellow_crystal));
			collected.Add (new LeafletItem ("Magenta", 0, sack.magenta_crystal));
			collected.Add (new LeafletItem ("White", 0, sack.white_crystal));

			return collected;
		}

		#endregion

		#region Delegate Methods

		#region Fixed Rules

		private double FixedRuleDelegateToDelegateRotate(ActivationCollection currentInput, Rule target)
		{
			return ((currentInput.Contains(InputMoveRotateThing, CurrentAgent.Parameters.MIN_ACTIVATION))) ? 1.0 : 0.0;
		}       

		private double FixedRuleDelegateToMoveToThing(ActivationCollection currentInput, Rule target)
		{
			return ((currentInput.Contains(InputMoveRotateThing, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
		}

		private double FixedRuleDelegateToGetThing(ActivationCollection currentInput, Rule target)
		{
			return ((currentInput.Contains(InputGetThing, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
		}

		private double FixedRuleDelegateToNothing(ActivationCollection currentInput, Rule target)
		{
			return ((currentInput.Contains(InputNothing, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
		}

		private double FixedRuleDelegateToAvoid(ActivationCollection currentInput, Rule target)
		{
			return ((currentInput.Contains(InputAvoid, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
		}

		private double FixedRuleDelegateToBuryThing(ActivationCollection currentInput, Rule target)
		{
			return ((currentInput.Contains(InputBuryThing, CurrentAgent.Parameters.MAX_ACTIVATION))) ? 1.0 : 0.0;
		}


		#endregion

		#region Run Thread Method

		private void RunThread(object obj)
		{
			Console.WriteLine("Starting Cognitive Cycle ... press CTRL-C to finish !");
			// Cognitive Cycle starts here getting sensorial information
			while (CurrentCognitiveCycle != MaxNumberOfCognitiveCycles)
			{   
				// Get current sensorial information                    
				IList<Thing> sensorialInformation = OnNewVisualSensorialInformation();

				// Make the perception
				SensoryInformation si = MakePerceptionFromSensorialInput(sensorialInformation);

				//Perceive the sensory information
				CurrentAgent.Perceive(si);

				//Choose an action
				ExternalActionChunk chosen = CurrentAgent.GetChosenExternalAction(si);

				// Get the selected action
				String actionLabel = chosen.LabelAsIComparable.ToString();
				ClarionAgentActionType actionType = (ClarionAgentActionType)Enum.Parse(typeof(ClarionAgentActionType), actionLabel, true);

				// Call the output event handler
				if (OnNewExternalActionSelected != null)
				{
					OnNewExternalActionSelected(actionType);
				}

				// Increment the number of cognitive cycles
				CurrentCognitiveCycle++;

				//Wait to the agent accomplish his job
				if (TimeBetweenCognitiveCycles > 0)
				{
					Thread.Sleep(TimeBetweenCognitiveCycles);
				}
			}
		}

		#endregion

		#endregion
	}
}
