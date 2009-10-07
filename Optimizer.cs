using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Optimization
{
	public abstract class Optimizer
	{
		public class Settings : Optimization.Settings
		{
			[XmlElement("max-iterations")]
			public uint MaxIterations;
			
			[XmlElement("population-size")]
			public uint PopulationSize;
		}
		
		IStore d_store;
		State d_state;
		
		List<Parameter> d_parameters;
		List<Boundary> d_boundaries;
		
		List<Solution> d_population;
		Solution d_best;
		
		uint d_currentIteration;
		
		Settings d_settings;
		
		public Optimizer()
		{
			d_state = new State();
			d_settings = new Settings();
			
			d_population = new List<Solution>();
			d_parameters = new List<Parameter>();
			d_boundaries = new List<Boundary>();
		}
		
		public void Initialize()
		{
			// Create the initial population
			InitializePopulation();
		}
		
		virtual public Fitness.IFitness CreateFitness()
		{
			return new Fitness.Default();
		}
		
		virtual protected Solution CreateSolution(uint id)
		{
			return new Solution(id, CreateFitness(), d_state);
		}
		
		virtual public void InitializePopulation()
		{
			d_population.Clear();
			
			// Create initial population
			for (uint idx = 0; idx < d_settings.PopulationSize; ++idx)
			{
				// Create new solution
				Solution solution = CreateSolution(idx);
				
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
		
		[XmlElement("parameters", typeof(Parameter))]
		public List<Parameter> Parameters
		{
			get
			{
				return d_parameters;
			}
		}
		
		[XmlElement("boundaries", typeof(Boundary))]
		public List<Boundary> Boundaries
		{
			get
			{
				return d_boundaries;
			}
		}
		
		public IStore Store
		{
			get
			{
				return d_store;
			}
			set
			{
				 d_store = value;
			}
		}
		
		public abstract string Name
		{
			get;
		}
		
		[XmlElement()]
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
			d_store.SaveIteration(this);

			// Increment the iteration number
			IncrementIteration();
			
			// Check if the optimization is finished
			if (Finished())
			{
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
