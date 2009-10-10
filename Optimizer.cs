/*
 *  Optimizer.cs - This file is part of optimization-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License as published by the 
 * Free Software Foundation; either version 2.1 of the License, or (at your 
 * option) any later version.
 * 
 * This library is distributed in the hope that it will be useful, but WITHOUT 
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License 
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace Optimization
{
	public class Optimizer : IEnumerable<Solution>
	{
		public class Settings : Optimization.Settings
		{
			[Setting("max-iterations", 30)]
			public uint MaxIterations;
			
			[Setting("population-size", 30)]
			public uint PopulationSize;
		}
		
		public class TypeClass : Attribute
		{
			Type d_type;
			
			public TypeClass()
			{
				d_type = null;
			}
			
			public TypeClass(Type type)
			{
				d_type = type;
			}
			
			public Type Type
			{
				get
				{
					return d_type;
				}
			}
		}
		
		public class SolutionClass : TypeClass
		{
			public SolutionClass()
			{
			}
			
			public SolutionClass(Type type) : base(type)
			{
			}
		}
		
		public class SettingsClass : TypeClass
		{
			public SettingsClass()
			{
			}
			
			public SettingsClass(Type type) : base(type)
			{
			}
		}
		
		public class StorageClass : TypeClass
		{
			public StorageClass()
			{
			}
			
			public StorageClass(Type type) : base(type)
			{
			}
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
		private ConstructorInfo d_solutionConstructor;
		
		public Optimizer()
		{
			d_fitness = new Fitness();

			d_settings = CreateSettings();
			d_state = new State(d_settings);
			
			d_population = new List<Solution>();

			d_parameters = new List<Parameter>();
			d_boundaries = new List<Boundary>();
			
			d_storage = CreateStorage();
		}
		
		public virtual void Initialize()
		{
			// Create the initial population
			InitializePopulation();
			
			d_storage.Begin();
		}
		
		private Type FindTypeClass(Type parent, Type attrType)
		{
			object[] attrs;
			TypeClass t;
			
			// Check for the attribute on the current type
			attrs = GetType().GetCustomAttributes(attrType, true);
			
			if (attrs.Length != 0)
			{
				t = attrs[0] as TypeClass;
				
				if (t.Type != null && t.Type.IsSubclassOf(parent))
				{
					return t.Type;
				}
			}
			
			Type potential = null;
			
			// Find subclasses
			foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
			{
				if (!type.IsSubclassOf(parent))
				{
					continue;
				}
				
				// Store potential subclass here if we don't find anything with the attribute
				potential = type;
				attrs = type.GetCustomAttributes(attrType, true);
				
				if (attrs.Length == 0)
				{
					continue;
				}
				
				t = attrs[0] as TypeClass;
				
				if (t.Type == null)
				{
					return type;
				}
				else
				{
					return t.Type;
				}
			}
			
			return potential;
		}
		
		protected virtual Settings CreateSettings()
		{
			Type type = FindTypeClass(typeof(Settings), typeof(SettingsClass));
			
			if (type != null)
			{
				object ret = type.GetConstructor(new Type[] {}).Invoke(new object[] {});
				
				return ret as Settings;
			}
			
			return new Settings();
		}
		
		protected virtual Storage.Storage CreateStorage()
		{
			Type type = FindTypeClass(typeof(Storage.Storage), typeof(StorageClass));
			
			if (type != null)
			{
				object ret = type.GetConstructor(new Type[] {}).Invoke(new object[] {});
				return ret as Storage.Storage;
			}
			
			return new Storage.SQLite();
		}
		
		protected virtual Solution CreateSolution(uint idx)
		{
			if (d_solutionConstructor == null)
			{
				Type type = FindTypeClass(typeof(Solution), typeof(SolutionClass));
				
				if (type != null)
				{
					d_solutionConstructor = type.GetConstructor(new Type[] {typeof(uint), typeof(Fitness), typeof(State)});
				}
			}
			
			if (d_solutionConstructor == null)
			{
				return new Solution(idx, d_fitness, d_state);
			}

			object ret = d_solutionConstructor.Invoke(new object[] {idx, d_fitness, d_state});
			return ret as Solution;
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
		
		public Solution Best
		{
			get
			{
				return d_best;
			}
		}
		
		public List<Boundary> Boundaries
		{
			get
			{
				return d_boundaries;
			}
		}
		
		public List<Solution> Population
		{
			get
			{
				return d_population;
			}
		}

		public List<Parameter> Parameters
		{
			get
			{
				return d_parameters;
			}
		}
		
		public IEnumerator<Solution> GetEnumerator()
		{
			foreach (Solution solution in d_population)
			{
				yield return solution;
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
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

		public State State
		{
			get
			{
				return d_state;
			}
		}

		public Fitness Fitness
		{
			get
			{
				return d_fitness;
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
		
		public Settings Configuration
		{
			get
			{
				return d_settings;
			}
		}
		
		public Boundary Boundary(string name)
		{
			foreach (Boundary boundary in d_boundaries)
			{
				if (boundary.Name == name)
				{
					return boundary;
				}
			}
			
			return null;
		}
		
		public virtual string Name
		{
			get
			{
				return GetType().Name.ToLower();
			}
		}
		
		protected virtual void UpdateBest()
		{
			foreach (Solution solution in d_population)
			{				
				if (d_best == null || solution.Fitness.Value > d_best.Fitness.Value)
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
		
		public void Log(string type, string format, params object[] args)
		{
			Log(type, String.Format(format, args));
		}
		
		public virtual void Log(string type, string str)
		{
			d_storage.Log(type, str);
		}
	}
}
