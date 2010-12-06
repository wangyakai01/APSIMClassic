﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection;
using CSGeneral;


/// <summary>
/// Encapculates an instance of a model (e.g. wheat, sorghum, manager).
/// </summary>
internal class ModelInstance
   {
   public Type ClassType;
   public XmlNode Node;
   public ModelInstance Parent;
   public object TheModel;
   public string Name
      {
      get 
         {
         string name = XmlHelper.Name(Node);
         int PosPeriod = name.IndexOf('.');
         if (PosPeriod != -1)
            return name.Substring(PosPeriod + 1);
         else
            return name;
         }
      }
   public Variable ModelAPI;
   public List<ModelInstance> Children = new List<ModelInstance>();
   public List<Variable> Inputs = new List<Variable>();
   public List<Variable> Outputs = new List<Variable>();
   public List<Variable> Params = new List<Variable>();
   public List<Variable> States = new List<Variable>();
   public List<ModelRef> Refs = new List<ModelRef>();
   public List<EventPublisher> Publishers = new List<EventPublisher>();
   public List<EventSubscriber> Subscribers = new List<EventSubscriber>();


   /// <summary>
   /// Resolve all [Ref] pointers.
   /// </summary>
   public void ResolveRefs()
      {
      foreach (ModelRef Ref in Refs)
         {
         object ModelReference = FindModel(Ref.Name);
         if (ModelReference == null)
            {
            if (!Ref.Optional)
               throw new Exception("Cannot find ref: " + Ref.Name);
            }
         else
            {
            if (ModelReference.GetType().ToString().Contains("Generic.List"))
               Ref.Values = ModelReference;
            else
               Ref.Value = ModelReference;
            }
         }
      foreach (ModelInstance Child in Children)
         Child.ResolveRefs();
      }

   public ModelInstance Root
      {
      get
         {
         ModelInstance I = this;
         while (I.Parent != null)
            I = I.Parent;
         return I;
         }
      }
   /// <summary>
   /// Go find a model instance using the specified NameToFind. This name can
   /// have path information. Returns null if not found.
   ///   A leading . indicates an absolute address. e.g. .simulation.paddock1.soilwat
   ///   A parent() indicates a parent directory.   e.g. parent().clock
   ///   No dot indicates a child.                  e.g. leaf 
   ///   A * indicates all children.
   ///   A parent(Plant) indicates go up the hierarchy looking for a parent called plant.                 
   /// </summary>
   public ModelInstance FindModelInstance(string NameToFind)
      {
      ModelInstance I = this;

      string[] Paths = NameToFind.Split(".".ToCharArray());
      int PathIndex = 0;
      while (PathIndex < Paths.Length)
         {
         string Path = Paths[PathIndex];
         if (Path == "")
            {
            // Must be a reference to the root node.
            I = Root;
            PathIndex++; // skip over the first path which should be the root node.
            }
         else if (Path == "parent()")
            {
            if (I.Parent == null)
               return null;
            I = I.Parent;
            }
         else if (Path.Contains("parent("))
            {
            string ParentNameToFind = StringManip.SplitOffBracketedValue(ref Path, '(', ')');
            if (Path.ToLower() != "parent")
               return null;
            while (I.Parent != null)
               {
               I = I.Parent;
               if (I.Name.ToLower() == ParentNameToFind.ToLower())
                  break;
               }
            if (I.Name.ToLower() != ParentNameToFind.ToLower())
               return null;
            }
         else
            {
            // Find a child node.
            int ChildIndex = 0;
            while (ChildIndex < I.Children.Count && I.Children[ChildIndex].Name.ToLower() != Path.ToLower())
               ChildIndex++;
            if (ChildIndex == I.Children.Count)
               return null;
            I = I.Children[ChildIndex];
            }

         PathIndex++;
         }
      return I;
      }

   public object FindModel(string NameToFind)
      {
      if (NameToFind == "*")
         return Children;

      ModelInstance I = FindModelInstance(NameToFind);
      if (I == null)
         return I;
      else
         return I.TheModel;
      }
   
   /// <summary>
   /// Find a specific output that matches "NameToFind". This method will search the
   /// instance passed in plus all child instances.
   /// </summary>
   public Variable FindOutput(string NameToFind)
      {
      // See if the instance passed in has the output we want.
      foreach (Variable Output in Outputs)
         {
         if (Output.Name.ToLower() == NameToFind.ToLower())
            return Output;
         }

      // If we get this far, then we haven't found the output so go check our children.
      foreach (ModelInstance Child in Children)
         {
         Variable Output = Child.FindOutput(NameToFind);
         if (Output != null)
            return Output;
         }
      return null;
      }
   public void UpdateValues()
      {
      foreach (Variable Input in Inputs)
         Input.UpdateValue();
      foreach (ModelInstance Child in Children)
         Child.UpdateValues();
      }

   internal void SetModelAPIs()
      {
      if (ModelAPI != null)
         ModelAPI.Value = new ModelAPI(this);
      foreach (ModelInstance Child in Children)
         Child.SetModelAPIs();
      }
   internal void GetAllOutputNames(ref List<string> VariableNames)
      {
      foreach (Variable Output in Outputs)
         VariableNames.Add(Name + "." + Output.Name);
      foreach (ModelInstance Child in Children)
         Child.GetAllOutputNames(ref VariableNames);
      }

   internal void GetAllOutputValues(ref List<object> Values)
      {
      foreach (Variable Output in Outputs)
         Values.Add(Output.Value);
      foreach (ModelInstance Child in Children)
         Child.GetAllOutputValues(ref Values);
      }

   internal bool ModelHasParam(string ParamName)
      {
      if (ParamName == "Script")
         return true;

      foreach (Variable Param in Params)
         {
         if (Param.Name.ToLower() == ParamName.ToLower())
            return true;
         }
      return false;
      }
   }
