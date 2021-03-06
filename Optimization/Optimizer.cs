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
using Optimization.Attributes;
using System.Xml;
using Biorob.Math;

namespace Optimization
{
	public class Optimizer : IEnumerable<Solution>
	{
		public class Settings : Optimization.Settings
		{
			public enum RepeatTaskCombineType
			{
				Average,
				Minimum,
				Maximum,
				Snr
			}

			[Setting("max-iterations", 60, Description="Maximum number of iterations")]
			public uint MaxIterations;
			[Setting("population-size", 30, Description="Solution population size")]
			public uint PopulationSize;
			[Setting("convergence-threshold", "0", Description="Threshold on minimum change in the objective function improvement over convergence-window measurements")]
			public string ConvergenceThreshold;
			[Setting("convergence-window", "10", Description="Window over which to measure fitness improvement for convergence")]
			public string ConvergenceWindow;
			[Setting("min-iterations", "20", Description="Minimum number of iterations before calculating convergence")]
			public string MinIterations;
			[Setting("initial-population", null, Description="Database file from which to generate the initial population")]
			public string InitialPopulation;
			[Setting("initial-population-noise", 0, Description="Noise to add to initial sampling from the initial population database")]
			public double InitialPopulationNoise;
			[Setting("repeat-task", 1, Description="Number of times to repeat a task")]
			public uint RepeatTask;
			[Setting("repeat-task-combine", RepeatTaskCombineType.Average, Description="Function computing how to combine fitness values from multiple task runs")]
			public RepeatTaskCombineType RepeatTaskCombine;
		}

		private Storage.Storage d_storage;
		private State d_state;
		private Fitness d_fitness;
		private List<Parameter> d_parameters;
		private List<Boundary> d_boundaries;
		private Dictionary<string, Boundary> d_boundaryHash;
		private Dictionary<string, Parameter> d_parameterHash;
		private List<Solution> d_population;
		private Solution d_best;
		private LinkedList<Fitness> d_lastBest;
		private uint d_currentIteration;
		private Settings d_settings;
		private ConstructorInfo d_solutionConstructor;
		private List<Extension> d_extensions;
		private Expression d_convergenceThreshold;
		private Expression d_convergenceWindow;
		private Expression d_minIterations;

		public Optimizer()
		{
			d_fitness = new Fitness();

			d_settings = CreateSettings();
			d_state = CreateState();

			d_population = new List<Solution>();

			d_parameters = new List<Parameter>();
			d_boundaries = new List<Boundary>();

			d_boundaryHash = new Dictionary<string, Boundary>();
			d_parameterHash = new Dictionary<string, Parameter>();

			d_extensions = new List<Extension>();

			d_convergenceThreshold = new Expression();
			d_convergenceWindow = new Expression();
			d_minIterations = new Expression();

			d_lastBest = new LinkedList<Fitness>();
		}

		public virtual void Initialize()
		{
			d_storage.Begin();
			
			if (d_settings.InitialPopulation != null)
			{
				Storage.Database db = new Storage.Database(d_settings.InitialPopulation);
				db.Open();

				d_storage.ImportTable(db, "initial_population");
				d_storage.ImportTable(db, "initial_population_data");

				db.Close();
			}
			
			// Create the initial population
			InitializePopulation();

			foreach (Extension ext in d_extensions)
			{
				ext.Initialize();
			}

			Setup();
		}

		private int TypeDistance(Type parent, Type child)
		{
			int ret = 0;

			while (!child.Equals(parent))
			{
				child = child.BaseType;
				++ret;
			}

			return ret;
		}

		private Type FindTypeClass(Type parent)
		{
			Type found = null;
			int distance = 0;

			// Find subclasses
			foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
			{
				if (!type.IsSubclassOf(parent))
				{
					continue;
				}

				// Store potential subclass here if we don't find anything with the attribute
				int dist = TypeDistance(parent, type);

				if (found == null || dist > distance)
				{
					found = type;
					distance = dist;
				}
			}

			return found;
		}

		public virtual uint NumId
		{
			get { return (uint)d_population.Count; }
		}

		public uint RealSolutionId(uint id)
		{
			if (d_settings.RepeatTask > 1)
			{
				return id % NumId;
			}
			else
			{
				return id;
			}
		}

		protected virtual Settings CreateSettings()
		{
			Type type = FindTypeClass(typeof(Settings));

			if (type != null)
			{
				object ret = type.GetConstructor(new Type[] {}).Invoke(new object[] {});

				return ret as Settings;
			}

			return new Settings();
		}

		protected virtual State CreateState()
		{
			Type type = FindTypeClass(typeof(State));

			if (type != null)
			{
				object ret = type.GetConstructor(new Type[] {typeof(Optimizer.Settings)}).Invoke(new object[] {d_settings});
				return ret as State;
			}

			return new State(d_settings);
		}

		public virtual Solution CreateSolution(uint idx)
		{
			if (d_solutionConstructor == null)
			{
				Type type = FindTypeClass(typeof(Solution));

				if (type != null)
				{
					d_solutionConstructor = type.GetConstructor(new Type[] {typeof(uint), typeof(Fitness), typeof(State)});
				}
			}

			Solution ret;

			if (d_solutionConstructor == null)
			{
				ret = new Solution(idx, d_fitness, d_state);
			}
			else
			{
				ret = (Solution)d_solutionConstructor.Invoke(new object[] {idx, d_fitness, d_state});
			}

			return ret;
		}
		
		private void SolutionFromInitial(Solution solution, Optimization.Storage.Records.InitialPopulation population)
		{
			int idx = (int)System.Math.Round(State.Random.NextDouble() * (population.Population.Count - 1));
			Optimization.Storage.Records.InitialSolution sol = population.Population[idx];
			
			foreach (Parameter parameter in solution.Parameters)
			{
				// skip parameters that are not in the initial database
				if (!sol.Parameters.ContainsKey(parameter.Name))
				{
					continue;
				}

				double val = sol.Parameters[parameter.Name];
				
				if (d_settings.InitialPopulationNoise > 0)
				{
					val += (State.Random.NextDouble() * 2 - 1) * d_settings.InitialPopulationNoise * (parameter.Boundary.Max - parameter.Boundary.Min);
					val = System.Math.Max(System.Math.Min(val, parameter.Boundary.Max), parameter.Boundary.Min);
				}
				
				parameter.Value = val;
			}
			
			foreach (KeyValuePair<string, string> pair in sol.Data)
			{
				solution.Data[pair.Key] = pair.Value;
			}
		}

		virtual public void InitializePopulation()
		{
			d_population.Clear();

			Optimization.Storage.Records.InitialPopulation initial = d_storage.ReadInitialPopulation();

			// Create initial population
			for (uint idx = 0; idx < d_settings.PopulationSize; ++idx)
			{
				// Create new solution
				Solution solution = CreateSolution(idx);

				// Set solution parameter template
				solution.Parameters = d_parameters;

				// Resetting the solution randomly initializes its parameters
				solution.Reset();
				
				if (initial != null)
				{
					// Initialize solution from the specified initial population
					SolutionFromInitial(solution, initial);
				}

				Add(solution);
			}

			foreach (Extension ext in d_extensions)
			{
				ext.InitializePopulation();
			}
		}

		virtual public void Add(Solution solution)
		{
			d_population.Add(solution);
		}

		virtual public void Remove(Solution solution)
		{
			d_population.Remove(solution);
		}

		public List<Extension> Extensions
		{
			get
			{
				return d_extensions;
			}
		}

		public virtual void AddExtension(Extension ext)
		{
			d_extensions.Add(ext);
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

		protected void AddBoundary(Boundary boundary)
		{
			Boundary existing;

			if (d_boundaryHash.TryGetValue(boundary.Name, out existing))
			{
				d_boundaries.Remove(existing);
				d_boundaryHash.Remove(boundary.Name);
			}

			d_boundaries.Add(boundary);
			d_boundaryHash.Add(boundary.Name, boundary);
		}

		public Boundary Boundary(string name)
		{
			return d_boundaryHash[name];
		}

		public bool HasBoundary(string name)
		{
			return d_boundaryHash.ContainsKey(name);
		}

		protected void AddParameter(Parameter parameter)
		{
			Parameter existing;

			if (d_parameterHash.TryGetValue(parameter.Name, out existing))
			{
				d_parameters.Remove(existing);
				d_parameterHash.Remove(parameter.Name);
			}

			d_parameters.Add(parameter);
			d_parameterHash.Add(parameter.Name, parameter);
		}

		public Parameter Parameter(string name)
		{
			return d_parameterHash[name];
		}

		public bool HasParameter(string name)
		{
			return d_parameterHash.ContainsKey(name);
		}

		public static string GetDescription(Type type)
		{
			object[] attr = type.GetCustomAttributes(typeof(OptimizerAttribute), false);

			if (attr.Length != 0)
			{
				return (attr[0] as OptimizerAttribute).Description;
			}

			return null;
		}

		public string Description
		{
			get
			{
				return GetDescription(GetType());
			}
		}

		public static string GetName(Type type)
		{
			object[] attr = type.GetCustomAttributes(typeof(OptimizerAttribute), false);
			string name = null;

			if (attr.Length != 0)
			{
				name = (attr[0] as OptimizerAttribute).Name;
			}

			if (name == null)
			{
				name = type.Name;
			}

			return name;
		}

		public string Name
		{
			get
			{
				return Optimizer.GetName(GetType());
			}
		}

		protected virtual void UpdateBest()
		{		
			foreach (Extension ext in d_extensions)
			{
				Solution best = ext.UpdateBest();
				
				if (best != null)
				{
					d_best = best;
					return;
				}
			}

			foreach (Solution solution in d_population)
			{
				if (d_best == null || solution.Fitness > d_best.Fitness)
				{
					d_best = solution.Clone() as Solution;
				}
			}
		}

		public virtual void UpdateFitness(Solution sol, Dictionary<string, double> fitness)
		{
			sol.Update(fitness);

			foreach (Extension ext in Extensions)
			{
				ext.UpdateFitness(sol);
			}
		}

		private double LastBestDifference
		{
			get
			{
				double min = double.MaxValue;
				double max = double.MinValue;

				foreach (Fitness fitness in d_lastBest)
				{
					if (fitness.Value < min)
					{
						min = fitness.Value;
					}

					if (fitness.Value > max)
					{
						max = fitness.Value;
					}
				}

				return max - min;
			}
		}
		
		public Biorob.Math.Expression MinIterations
		{
			get
			{
				return d_minIterations;
			}
		}
		
		public Biorob.Math.Expression ConvergenceThreshold
		{
			get
			{
				return d_convergenceThreshold;
			}
		}
		
		public Biorob.Math.Expression ConvergenceWindow
		{
			get
			{
				return d_convergenceWindow;
			}
		}

		protected virtual bool Finished()
		{
			if (d_currentIteration >= d_settings.MaxIterations && d_settings.MaxIterations > 0)
			{
				return true;
			}
			
			bool suppress = false;

			foreach (Extension ext in d_extensions)
			{
				if (ext.Finished())
				{
					return true;
				}
				
				if (ext.SuppressConvergenceCalculation())
				{
					suppress = true;
				}
			}
			
			if (suppress)
			{
				return false;
			}

			uint minIterations = (uint)d_minIterations.Evaluate(Biorob.Math.Constants.Context);

			if (CurrentIteration < minIterations)
			{
				return false;
			}

			double threshold = d_convergenceThreshold.Evaluate(Biorob.Math.Constants.Context);
			uint window = (uint)d_convergenceWindow.Evaluate(Biorob.Math.Constants.Context);

			if (threshold > 0 && CurrentIteration > window)
			{
				return LastBestDifference < threshold;
			}

			return false;
		}

		protected virtual void IncrementIteration()
		{
			d_currentIteration++;
		}

		private void UpdateConvergence()
		{
			uint window = (uint)d_convergenceWindow.Evaluate(Biorob.Math.Constants.Context);

			while (d_lastBest.Count > window)
			{
				d_lastBest.RemoveFirst();
			}

			Fitness fitness = null;

			foreach (Solution solution in Population)
			{
				if (fitness == null || solution.Fitness > fitness)
				{
					fitness = solution.Fitness;
				}
			}

			if (fitness != null)
			{
				d_lastBest.AddLast((Fitness)fitness.Clone());
			}
		}
		
		protected virtual void SaveIteration()
		{
			d_storage.SaveIteration();
		}

		public virtual bool Next()
		{
			foreach (Extension ext in d_extensions)
			{
				ext.Next();
			}

			// Update convergence
			UpdateConvergence();

			// First update the best solution up until now
			UpdateBest();

			// Then tell the store to save the current iteration
			SaveIteration();
			
			// Increment the iteration number
			IncrementIteration();

			// Check if the optimization is finished
			if (Finished())
			{
				Log("status", "Finished optimization");
				d_storage.End();
				return false;
			}

			// Update the population to make new solutions
			Update();

			return true;
		}

		public virtual void Update()
		{
			foreach (Extension ext in d_extensions)
			{
				ext.BeforeUpdate();
			}

			foreach (Solution solution in d_population)
			{
				Update(solution);
			}

			foreach (Extension ext in d_extensions)
			{
				ext.AfterUpdate();
			}
		}

		public virtual void Update(Solution solution)
		{
			foreach (Extension ext in d_extensions)
			{
				ext.Update(solution);
			}
		}

		public void Log(string type, string format, params object[] args)
		{
			Log(type, String.Format(format, args));
		}

		public virtual void Log(string type, string str)
		{
			d_storage.Log(type, str);
		}

		public virtual void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer, Storage.Records.Solution solution, Optimization.Solution sol)
		{
			sol.Parameters.Clear();

			foreach (KeyValuePair<string, double> parameter in solution.Parameters)
			{
				sol.Parameters.Add(new Parameter(parameter.Key, parameter.Value, Parameter(parameter.Key).Boundary));
			}

			sol.FromStorage(storage, optimizer, solution);
		}

		protected virtual void Setup()
		{
			d_convergenceThreshold.Parse(Configuration.ConvergenceThreshold);
			d_convergenceWindow.Parse(Configuration.ConvergenceWindow);
			d_minIterations.Parse(Configuration.MinIterations);
		}

		public virtual void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			d_storage = storage;

			if (optimizer == null)
			{
				optimizer = d_storage.ReadJob().Optimizer;
			}

			/* Settings */
			d_settings.Clear();

			foreach (KeyValuePair<string, string> pair in optimizer.Settings)
			{
				d_settings[pair.Key] = pair.Value;
			}

			/* Boundaries */
			d_boundaries.Clear();
			d_boundaryHash.Clear();

			foreach (Storage.Records.Boundary boundary in optimizer.Boundaries)
			{
				Boundary bound = new Boundary(boundary.Name);

				bound.MinSetting.Representation = boundary.Min;
				bound.MaxSetting.Representation = boundary.Max;
				bound.MinInitialSetting.Representation = boundary.MinInitial;
				bound.MaxInitialSetting.Representation = boundary.MaxInitial;

				AddBoundary(bound);
			}

			/* Parameters */
			d_parameters.Clear();

			foreach (Storage.Records.Parameter parameter in optimizer.Parameters)
			{
				AddParameter(new Parameter(parameter.Name, 0, Boundary(parameter.Boundary.Name)));
			}

			/* Fitness */
			d_fitness.Clear();

			Fitness.Mode mode = Fitness.ModeFromString(optimizer.Fitness.Mode);

			if (mode != Fitness.Mode.Invalid)
			{
				Fitness.CompareMode = mode;
			}
			else
			{
				Fitness.CompareMode = Fitness.Mode.Default;
			}

			d_fitness.Expression.Parse(optimizer.Fitness.Expression);

			foreach (KeyValuePair<string, Storage.Records.Fitness.Variable> pair in optimizer.Fitness.Variables)
			{
				mode = Fitness.CompareMode;

				Fitness.Mode parsed = Fitness.ModeFromString(pair.Value.Mode);

				if (parsed != Fitness.Mode.Invalid)
				{
					mode = parsed;
				}

				d_fitness.AddVariable(pair.Key, pair.Value.Expression, parsed);
			}

			/* Restore iteration, state */
			d_currentIteration = (uint)storage.ReadIterations();

			d_state.Settings.Clear();

			foreach (KeyValuePair<string, string> pair in optimizer.State.Settings)
			{
				d_state.Settings[pair.Key] = pair.Value;
			}

			d_state.Random = optimizer.State.Random;

			/* Restore population */
			d_population.Clear();

			if (d_currentIteration > 0)
			{
				Storage.Records.Iteration iteration = storage.ReadIteration((int)d_currentIteration - 1);

				foreach (Storage.Records.Solution solution in iteration.Solutions)
				{
					Solution sol = CreateSolution((uint)solution.Index);
					FromStorage(storage, optimizer, solution, sol);

					Add(sol);
				}

				// Restore best solution
				Storage.Records.Solution best = storage.ReadSolution(-1, -1);

				if (best != null)
				{
					d_best = CreateSolution((uint)best.Index);
					FromStorage(storage, optimizer, best, d_best);
				}
				else
				{
					d_best = null;
				}
			}
			else
			{
				InitializePopulation();
				d_best = null;
			}

			foreach (Extension ext in d_extensions)
			{
				ext.FromStorage(storage, optimizer);
			}

			Setup();
		}

		public virtual void FromXml(XmlNode node)
		{
			LoadSettings(node);
			LoadBoundaries(node);
			LoadParameters(node);
			LoadFitness(node);

			foreach (Extension ext in d_extensions)
			{
				ext.FromXml(node);
			}
		}

		private void LoadSettings(XmlNode root)
		{
			XmlNodeList nodes = root.SelectNodes("setting");

			foreach (XmlNode node in nodes)
			{
				XmlAttribute attr = node.Attributes["name"];

				if (attr != null)
				{
					d_settings[attr.Value] = node.InnerText;
				}
				else
				{
					throw new Exception("XML: Optimizer setting has no name");
				}
			}
		}

		private Boundary CreateBoundary(string name, XmlNode node)
		{
			XmlAttribute min = node.Attributes["min"];
			XmlAttribute max = node.Attributes["max"];
			XmlAttribute minInitial = node.Attributes["min-initial"];
			XmlAttribute maxInitial = node.Attributes["max-initial"];

			if (min == null)
			{
				throw new Exception(String.Format("XML: No minimum value specified for boundary: {0}", name));
			}
			else if (max == null)
			{
				throw new Exception(String.Format("XML: No maximum value specified for boundary: {0}", name));
			}

			Optimization.Boundary boundary = new Optimization.Boundary(name);

			try
			{
				boundary.MinSetting.Representation = min.Value;
			}
			catch
			{
				throw new Exception(String.Format("XML: Could not parse minimum boundary value: {0} ({1})", name, min.Value));
			}

			try
			{
				boundary.MaxSetting.Representation = max.Value;
			}
			catch
			{
				throw new Exception(String.Format("XML: Could not parse maximum boundary value: {0} ({1})", name, max.Value));
			}

			if (boundary.Max < boundary.Min)
			{
				throw new Exception(String.Format("XML: Maximum boundary value is smaller than minimum value: {0} => [{1}, {2}]", name, min.Value, max.Value));
			}

			try
			{
				if (maxInitial != null)
				{
					boundary.MaxInitialSetting.Representation = maxInitial.Value;
				}
				else
				{
					boundary.MaxInitialSetting.Representation = boundary.MaxSetting.Representation;
				}
			}
			catch
			{
				throw new Exception(String.Format("XML: Could not parse maximum initial boundary value: {0} ({1})", name, maxInitial.Value));
			}

			if (boundary.MaxInitial > boundary.Max)
			{
				throw new Exception(String.Format("XML: Maximum initial value is larger than maximum value: {0}", name));
			}

			try
			{
				if (minInitial != null)
				{
					boundary.MinInitialSetting.Representation = minInitial.Value;
				}
				else
				{
					boundary.MinInitialSetting.Representation = boundary.MinSetting.Representation;
				}
			}
			catch
			{
				throw new Exception(String.Format("XML: Could not parse minimum initial boundary value: {0} ({1})", name, minInitial.Value));
			}

			if (boundary.MinInitial < boundary.Min)
			{
				throw new Exception(String.Format("XML: Minimum initial value is smaller than minimum value: {0}, {1} < {2}", name, boundary.MinInitial, boundary.Min));
			}

			if (boundary.MaxInitial < boundary.MinInitial)
			{
				throw new Exception(String.Format("XML: Maximum initial value is smaller than minimum initial value: {0}", name));
			}

			return boundary;
		}

		private void LoadBoundaries(XmlNode root)
		{
			XmlNodeList nodes = root.SelectNodes("boundaries/boundary");

			foreach (XmlNode node in nodes)
			{
				XmlAttribute nm = node.Attributes["name"];

				if (nm == null)
				{
					throw new Exception("XML: No name specified for boundary");
				}

				AddBoundary(CreateBoundary(nm.Value, node));
			}
		}

		private void LoadParameters(XmlNode root)
		{
			XmlNodeList nodes = root.SelectNodes("parameters/parameter");

			foreach (XmlNode node in nodes)
			{
				XmlAttribute nm = node.Attributes["name"];
				XmlAttribute bound = node.Attributes["boundary"];

				Boundary boundary = null;

				if (nm == null)
				{
					throw new Exception("XML: Invalid parameter specification, has no name");
				}
				else if (bound == null)
				{
					if (node.Attributes["min"] != null && node.Attributes["max"] != null)
					{
						string boundaryName = nm.Value;
						int cnt = 0;

						while (HasBoundary(boundaryName))
						{
							boundaryName = (new String('_', ++cnt)) + nm.Value;
						}

						boundary = CreateBoundary(boundaryName, node);
						AddBoundary(boundary);
					}
					else
					{
						throw new Exception(String.Format("XML: Invalid parameter specification {0}, no boundary specified", nm.Value));
					}
				}
				else
				{
					if (!HasBoundary(bound.Value))
					{
						throw new Exception(String.Format("XML: Invalid parameter specification {0}, could not find boundary {1}", nm.Value, bound.Value));
					}

					boundary = Boundary(bound.Value);
				}

				if (node.Attributes["repeat"] != null)
				{
					string range = node.Attributes["repeat"].Value;
					string[] parts = range.Split('-');

					if (parts.Length != 2)
					{
						throw new Exception(String.Format("XML: Invalid range specification `{0}' for `{1}'", range, nm.Value));
					}

					int start = Int32.Parse(parts[0]);
					int end = Int32.Parse(parts[1]);

					while (start <= end)
					{
						AddParameter(new Parameter(String.Format("{0}{1}", nm.Value, start), boundary));
						++start;
					}
				}
				else
				{
					AddParameter(new Parameter(nm.Value, boundary));
				}
			}

		}

		private void LoadFitness(XmlNode root)
		{
			XmlNode fitness = root.SelectSingleNode("fitness");
			Fitness.CompareMode = Fitness.Mode.Default;

			if (fitness == null)
			{
				return;
			}

			XmlAttribute attr = fitness.Attributes["mode"];

			if (attr != null)
			{
				Fitness.Mode mode = Fitness.ModeFromString(attr.Value);

				if (mode == Fitness.Mode.Invalid)
				{
					throw new Exception(String.Format("Fitness mode is invalid: `{0}'", attr.Value));
				}

				Fitness.CompareMode = mode;
			}

			XmlNode expression = root.SelectSingleNode("fitness/expression");

			if (expression == null)
			{
				return;
			}

			if (!d_fitness.Parse(expression.InnerText))
			{
				throw new Exception("XML: Could not parse fitness");
			}

			XmlNodeList nodes = root.SelectNodes("fitness/variable");

			foreach (XmlNode node in nodes)
			{
				XmlAttribute nm = node.Attributes["name"];

				if (nm == null)
				{
					throw new Exception("XML: Fitness variable has no name");
				}

				XmlAttribute hint = node.Attributes["mode"];

				Fitness.Mode mode = Fitness.CompareMode;

				if (hint != null)
				{
					Fitness.Mode parsed = Fitness.ModeFromString(hint.Value);

					if (parsed != Fitness.Mode.Invalid)
					{
						mode = parsed;
					}
				}

				d_fitness.AddVariable(nm.Value, node.InnerText, mode);
			}
		}
	}
}
