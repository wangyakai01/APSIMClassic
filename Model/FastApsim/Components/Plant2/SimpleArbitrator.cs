using System;
using System.Collections.Generic;
using System.Text;
using CSGeneral;


public class SimpleArbitrator :  Arbitrator
   {
   [Param] private string DMSink = null;
   private double TotalDMDemand = 0;
   private double TotalDMSupply = 0;
   private double TotalAllocated = 0;
   private double TotalDMRetranslocationSupply = 0;

   [Param] private string NSink = null;
   private double TotalNDemand = 0;
   private double TotalNSupply = 0;
   //private double TotalAllocated = 0;
   private double TotalNRetranslocationSupply = 0;
   [Ref("parent(Plant)")] Plant Plant;

   [Output] public override double DMSupply
      {
      get
         {
         return TotalDMSupply;
         }
      }
   [Output]
   public override double NDemand
      {
      get
         {
         return TotalNDemand;
         }
      }
   public override void DoDM(Organ[] Organs)
      {
      double [] DMRetranslocationSupply = new double[Organs.Length];
      double[] DMDemand = new double[Organs.Length];
      double[] DMSupply = new double[Organs.Length];
      double[] DMAllocation = new double[Organs.Length];
      double[] DMRetranslocation = new double[Organs.Length];

      TotalDMDemand = 0;
      TotalDMSupply = 0;
      TotalAllocated = 0;
      TotalDMRetranslocationSupply = 0;

      // Grab some state data for mass balance checking at end
      double StartingMass = Plant.AboveGroundDM + Plant.BelowGroundDM+Plant.ReserveDM;

      // Get required data from all organs and prepare for arbirtration

      for (int i = 0; i < Organs.Length; i++)
         DMSupply[i] = Organs[i].DMSupply;
      TotalDMSupply = MathUtility.Sum(DMSupply);

      for (int i = 0; i < Organs.Length; i++)
         DMDemand[i] = Organs[i].DMDemand;
      TotalDMDemand = MathUtility.Sum(DMDemand);

      for (int i = 0; i < Organs.Length; i++)
         DMAllocation[i] = 0;

      for (int i = 0; i < Organs.Length; i++)
         DMRetranslocationSupply[i] = Organs[i].DMRetranslocationSupply;
      TotalDMRetranslocationSupply = MathUtility.Sum(DMRetranslocationSupply);

      double fraction = 0;
      if (TotalDMDemand > 0)
         fraction = Math.Min(1, TotalDMSupply / TotalDMDemand);
      double Excess = Math.Max(0, TotalDMSupply - TotalDMDemand);


      // Allocate Daily Photosyntheis to organs according to demand

      for (int i = 0; i < Organs.Length; i++)
         {
         DMAllocation[i] =fraction * DMDemand[i];
         if (Organs[i].Name == DMSink)
            DMAllocation[i] += Excess;
         TotalAllocated += DMAllocation[i];
         }
      double BalanceError = Math.Abs(TotalAllocated - TotalDMSupply);

      if (BalanceError > 0.00001)
         {
         throw new Exception("Mass Balance Error in DM Allocation");
         }

      // Determine unmet demand of reproductive organs and retranslocate from
      // other organs as required/allowed.

      double TotalUnmetDemand = 0;
      for(int i=0;i<Organs.Length;i++)
            TotalUnmetDemand += DMDemand[i] - DMAllocation[i];

      double RetransDemandFraction = 0;
      if (TotalUnmetDemand > 0)
         RetransDemandFraction = Math.Min(1, TotalDMRetranslocationSupply / TotalUnmetDemand);

      double RetransSupplyFraction = 0;
      if (TotalDMRetranslocationSupply>0)
         RetransSupplyFraction=Math.Min(1,TotalUnmetDemand * RetransDemandFraction/TotalDMRetranslocationSupply);

      // Allocate Daily Retranslocation to organs according to demand and Supply

      for (int i = 0; i < Organs.Length; i++)
         {
         DMAllocation[i] += RetransDemandFraction * (DMDemand[i] - DMAllocation[i]);
         DMRetranslocation[i] = DMRetranslocationSupply[i] * RetransSupplyFraction;
         TotalAllocated += DMAllocation[i];
         }
      
      // Now Send Arbitration Results to all Plant Organs
      for (int i = 0; i < Organs.Length; i++)
         {
         Organs[i].DMAllocation = DMAllocation[i];
         Organs[i].DMRetranslocation = DMRetranslocation[i];
         }




      /// Now check that everything still adds up!!!!
      double EndMass = Plant.AboveGroundDM + Plant.BelowGroundDM+Plant.ReserveDM;
      BalanceError = Math.Abs(EndMass - StartingMass - TotalDMSupply);

      if (BalanceError > 0.01)
         {
         throw new Exception("Mass Balance Error in DM Allocation");
         }


      }
   public override void DoN(Organ[] Organs)
      {
      double[] NRetranslocationSupply = new double[Organs.Length];
      double[] NDemand = new double[Organs.Length];
      double[] NSupply = new double[Organs.Length];
      double[] NAllocation = new double[Organs.Length];
      double[] NRetranslocation = new double[Organs.Length];

      TotalNDemand = 0;
      TotalNSupply = 0;
      TotalAllocated = 0;
      TotalNRetranslocationSupply = 0;

      // Grab some state data for mass balance checking at end
      double StartingN = Plant.AboveGroundN + Plant.BelowGroundN + Plant.ReserveN;

      // Get required data from all organs and prepare for arbirtration

      for (int i = 0; i < Organs.Length; i++)
         NDemand[i] = Organs[i].NDemand;
      TotalNDemand = MathUtility.Sum(NDemand);

      for (int i = 0; i < Organs.Length; i++)
         NSupply[i] = Organs[i].NUptakeSupply;
      TotalNSupply = MathUtility.Sum(NSupply);
      for (int i = 0; i < Organs.Length; i++)
         NAllocation[i] = 0;

      for (int i = 0; i < Organs.Length; i++)
         NRetranslocationSupply[i] = Organs[i].NRetranslocationSupply;
      TotalNRetranslocationSupply = MathUtility.Sum(NRetranslocationSupply);

      double fraction = 0;
      if (TotalNDemand > 0)
         fraction = Math.Min(1, TotalNSupply / TotalNDemand);
      double Excess = Math.Max(0, TotalNSupply - TotalNDemand);


      // Allocate Daily Photosyntheis to organs according to demand

      for (int i = 0; i < Organs.Length; i++)
         {
         NAllocation[i] = fraction * NDemand[i];
         if (Organs[i].Name == NSink)
            NAllocation[i] += Excess;
         TotalAllocated += NAllocation[i];
         }
      double BalanceError = Math.Abs(TotalAllocated - TotalNSupply);

      if (BalanceError > 0.00001)
         {
         throw new Exception("Mass Balance Error in N Allocation");
         }

      // Determine unmet demand of reproductive organs and retranslocate from
      // other organs as required/allowed.

      double TotalUnmetDemand = 0;
      for (int i = 0; i < Organs.Length; i++)
         TotalUnmetDemand += NDemand[i] - NAllocation[i];

      double RetransDemandFraction = 0;
      if (TotalUnmetDemand > 0)
         RetransDemandFraction = Math.Min(1, TotalNRetranslocationSupply / TotalUnmetDemand);

      double RetransSupplyFraction = 0;
      if (TotalNRetranslocationSupply > 0)
         RetransSupplyFraction = Math.Min(1, TotalUnmetDemand * RetransDemandFraction / TotalNRetranslocationSupply);

      // Allocate Daily Retranslocation to organs according to demand and Supply

      for (int i = 0; i < Organs.Length; i++)
         {
         NAllocation[i] += RetransDemandFraction * (NDemand[i] - NAllocation[i]);
         NRetranslocation[i] = NRetranslocationSupply[i] * RetransSupplyFraction;
         TotalAllocated += NAllocation[i];
         }

      // Now Send Arbitration Results to all Plant Organs
      for (int i = 0; i < Organs.Length; i++)
         {
         Organs[i].NAllocation = NAllocation[i];
         Organs[i].NRetranslocation = NRetranslocation[i];
         }


      /// Now check that everything still adds up!!!!
      double EndN = Plant.AboveGroundN + Plant.BelowGroundN + Plant.ReserveN;
      BalanceError = Math.Abs(EndN - StartingN - TotalNSupply);

      if (BalanceError > 0.01)
         {
         throw new Exception("Mass Balance Error in N Allocation");
         }


      }

   public void DoDMold(Organ[] Organs)
      {
      double[] DMRetranslocationSupply = new double[Organs.Length];
      double[] DMDemand = new double[Organs.Length];
      double[] DMSupply = new double[Organs.Length];
      double[] DMAllocation = new double[Organs.Length];
      double[] DMRetranslocation = new double[Organs.Length];
      double TotalDMDemand = 0;
      double TotalDMSupply = 0;
      double TotalAllocated = 0;
      double TotalDMRetranslocationSupply = 0;

      // Grab some state data for mass balance checking at end
      double StartingMass = Plant.AboveGroundDM + Plant.BelowGroundDM;

      // Get required data from all organs and prepare for arbirtration

      for (int i = 0; i < Organs.Length; i++)
         DMSupply[i] = Organs[i].DMSupply;
      TotalDMSupply = MathUtility.Sum(DMSupply);

      for (int i = 0; i < Organs.Length; i++)
         DMDemand[i] = Organs[i].DMDemand;
      TotalDMDemand = MathUtility.Sum(DMDemand);

      for (int i = 0; i < Organs.Length; i++)
         DMAllocation[i] = 0;

      for (int i = 0; i < Organs.Length; i++)
         DMRetranslocationSupply[i] = Organs[i].DMRetranslocationSupply;
      TotalDMRetranslocationSupply = MathUtility.Sum(DMRetranslocationSupply);

      double fraction = 0;
      if (TotalDMDemand > 0)
         fraction = Math.Min(1, TotalDMSupply / TotalDMDemand);
      double Excess = Math.Max(0, TotalDMSupply - TotalDMDemand);


      // Allocate Daily Photosyntheis to organs according to demand

      for (int i = 0; i < Organs.Length; i++)
         {
         DMAllocation[i] = fraction * DMDemand[i];
         if (Organs[i].Name == DMSink)
            DMAllocation[i] += Excess;
         TotalAllocated += DMAllocation[i];
         }
      double BalanceError = Math.Abs(TotalAllocated - TotalDMSupply);

      if (BalanceError > 0.00001)
         {
         throw new Exception("Mass Balance Error in DM Allocation");
         }

      // Determine unmet demand of reproductive organs and retranslocate from
      // other organs as required/allowed.

      double TotalUnmetReproductiveDemand = 0;
      for (int i = 0; i < Organs.Length; i++)
         if (Organs[i] is Reproductive)
            TotalUnmetReproductiveDemand += DMDemand[i] - DMAllocation[i];

      double RetransDemandFraction = 0;
      if (TotalUnmetReproductiveDemand > 0)
         RetransDemandFraction = Math.Min(1, TotalDMRetranslocationSupply / TotalUnmetReproductiveDemand);

      double RetransSupplyFraction = 0;
      if (TotalDMRetranslocationSupply > 0)
         RetransSupplyFraction = Math.Min(1, TotalUnmetReproductiveDemand * RetransDemandFraction / TotalDMRetranslocationSupply);

      // Allocate Daily Retranslocation to organs according to demand and Supply

      for (int i = 0; i < Organs.Length; i++)
         {
         if (Organs[i] is Reproductive)
            DMAllocation[i] += RetransDemandFraction * (DMDemand[i] - DMAllocation[i]);
         DMRetranslocation[i] = DMRetranslocationSupply[i] * RetransSupplyFraction;
         TotalAllocated += DMAllocation[i];
         }



      // Now Send Arbitration Results to all Plant Organs
      for (int i = 0; i < Organs.Length; i++)
         {
         Organs[i].DMAllocation = DMAllocation[i];
         Organs[i].DMRetranslocation = DMRetranslocation[i];
         }




      /// Now check that everything still adds up!!!!
      double EndMass = Plant.AboveGroundDM + Plant.BelowGroundDM;
      BalanceError = Math.Abs(EndMass - StartingMass - TotalDMSupply);

      if (BalanceError > 0.00001)
         {
         throw new Exception("Mass Balance Error in DM Allocation");
         }


      }

   }

