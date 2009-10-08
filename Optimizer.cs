using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Optimization
{
	[XmlType("optimizer")]
	public abstract class Optimizer
	{
		public class Settings : Optimization.Settings
		{
			[XmlElement("max-iterations")]
			public uint MaxIterations;
			
			[XmlElement("population-size")]
			public uint PopulationSize;
		}
		
		private Storage.Storage d_storage;
		
		private State d_state;
		private Fitness d_fitness;
		
		private List<Parameter> d_parameters;
		private List<Boundary> d_boundaries;
		
		private List<Solution> d_population;
		private Solution d_best;
		
		private uint d_currentIteration;
		
		private Settings d_settings;
		
		public Optimizer()
		{
			d_state = new State();
			d_fitness = new Fitness();

			d_settings = SettingsFactory();
			
			d_population = new List<Solution>();
			d_parameters = new List<Parameter>();
			d_boundaries = new List<Boundary>();
		}
		
		public void Initialize()
		{
			// Create the initial population
			InitializePopulation();
			
			d_storage.Begin();
		}
		
		protected Settings SettingsFactory()
		{
			return new Settings();
		}
		
		protected Solution SolutionFactory(uint idx)
		{
			return new Solution(idx, d_fitness, d_state);
		}
		
		virtual public void InitializePopulation()
		{
			d_population.Clear();
			
			// Create initial population
			for (uint idx = 0; idx < d_settings.PopulationSize; ++idx)
			{
				// Create new solution
				Solution solution = SolutionFactory(idx);
				
				// Set solution parameter template
				solution.Parameters = d_parameters;
				
				// Resetting the solution randomly initializes its parameters
				solution.Reset();

				Add(solution);
			}
		}
		
		virtual public void Add(Solution solution)
		{
			d_population.Add(solution);
		}
		
		[XmlArray("boundaries")]
		public List<Boundary> Boundaries
		{
			get
			{
				return d_boundaries;
			}
		}

		[XmlArray("parameters")]
		public List<Parameter> Parameters
		{
			get
			{
				return d_parameters;
			}
		}

		[XmlIgnore()]
		public Storage.Storage Storage
		{
			get
			{
				return d_storage;
			}
			set
			{
				 d_storage = value;
			}
		}
		
		[XmlIgnore()]
		public State State
		{
			get
			{
				return d_state;
			}
		}
		
		[XmlIgnore()]
		public Fitness Fitness
		{
			get
			{
				return d_fitness;
			}
		}
		
		[XmlElement("configuration")]
		public Settings Configuration
		{
			get
			{
				return d_settings;
			}
			set
			{
				d_settings = value;
			}
		}
		
		[XmlIgnore()]
		public uint CurrentIteration
		{
			get
			{
				return d_currentIteration;
			}
			set
			{
				d_currentIteration = value;
			}
		}
		
		
		protected virtual void UpdateBest()
		{
			foreach (Solution solution in d_population)
			{
				if (solution.Fitness.Value > d_best.Fitness.Value)
				{
					d_best = solution.Clone() as Solution;
				}
			}
		}
		
		protected virtual bool Finished()
		{
			return d_currentIteration >= d_settings.MaxIterations;
		}
		
		protected virtual void IncrementIteration()
		{
			d_currentIteration++;
		}
		
		public virtual bool Next()
		{
			// First update the best solution up until now
			UpdateBest();
			
			// Then tell the store to save the current iteration
			d_storage.SaveIteration();

			// Increment the iteration number
			IncrementIteration();
			
			// Check if the optimization is finished
			if (Finished())
			{
				d_storage.End();
				return false;
			}
		
			// Update the population to make new solutions
			Update();
			return true;
		}
		
		public virtual void Update()
		{
			foreach (Solution solution in d_population)
			{
				Update(solution);
			}
		}
		
		public virtual void Update(Solution solution)
		{
			// NOOP
		}
	}
}
