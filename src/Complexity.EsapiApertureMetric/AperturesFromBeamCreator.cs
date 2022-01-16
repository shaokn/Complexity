using System;
using System.Collections.Generic;
using System.Linq;
using Complexity.ApertureMetric;
using Complexity.AriaEntity;
using VMS.TPS.Common.Model.API;
using Patient = VMS.TPS.Common.Model.API.Patient;
using PlanSetup = VMS.TPS.Common.Model.API.PlanSetup;

namespace Complexity.EsapiApertureMetric
{
    public class AperturesFromBeamCreator
    {
        public IEnumerable<Aperture> Create(Patient patient, PlanSetup plan, Beam beam)
        {
            List<Aperture> apertures = new List<Aperture>();
            double[] leafWidths = GetLeafWidths(patient, plan, beam);

            foreach (ControlPoint controlPoint in beam.ControlPoints)
            {
                double[,] leafPositions = GetLeafPositions(controlPoint);
                double[] jaw = CreateJaw(controlPoint);
                apertures.Add(new Aperture(leafPositions, leafWidths, jaw));
            }

            return apertures;
        }

        private double[] CreateJaw(ControlPoint cp)
        {
            double left   = cp.JawPositions.X1;
            double top    = cp.JawPositions.Y2;
            double right  = cp.JawPositions.X2;
            double bottom = cp.JawPositions.Y1;

            return new double[] { left, top, right, bottom };
        }

        public double[,] GetLeafPositions(ControlPoint controlPoint, Beam beam)
        {
            int m = controlPoint.LeafPositions.GetLength(0);
            int n = controlPoint.LeafPositions.GetLength(1);

            double[,] leafPositions = new double[m, n];

            if (!beam.TreatmentUnit.Id.Contains("Halcyon"))  //Stupid way of checking if machine is Halcyon
            {
                //convert from float to double and reorder
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        // Leaf positions are given from bottom to top by ESAPI,
                        // but the Aperture class expects them from top to bottom
                        leafPositions[i, j] = controlPoint.LeafPositions[i, n - j - 1];
                    }
                }
                return leafPositions;
            }

            else
            {
                //convert from float to double
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        leafPositions[i, j] = controlPoint.LeafPositions[i, j];
                    }
                }
                double[,] leafPositionsHalcyon = new double[2, 56];
                // 5 mm leaf -> formed from original table
                // 1 -> 1+29
                // 2 -> 1+30
                // 3 -> 2+30
                // 4 -> 2+31
                // 5 -> 3+31
                // 6 -> 3+32
                // 7 -> 4+32
                // 8 -> 4+33
                // 9 -> 5+33
                //...
                int[] a = new int[] { 0,  0,  1,  1,  2,  2,  3,  3,  4,  4,  5,  5,  6,  6,  7,  7,  8,
                                        8,  9,  9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16,
                                        17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22, 23, 23, 24, 24, 25,
                                        25, 26, 26, 27, 27};
                int[] b = new int[] {28, 29, 29, 30, 30, 31, 31, 32, 32, 33, 33, 34, 34, 35, 35, 36, 36,
                                        37, 37, 38, 38, 39, 39, 40, 40, 41, 41, 42, 42, 43, 43, 44, 44, 45,
                                        45, 46, 46, 47, 47, 48, 48, 49, 49, 50, 50, 51, 51, 52, 52, 53, 53,
                                        54, 54, 55, 55, 56};

                for (int j = 0; j < 56; j++)
                {
                    int ai = a[j];
                    int bi = b[j];

                    if (leafPositions[0, ai] < leafPositions[0, bi])
                    {
                        leafPositionsHalcyon[0, j] = leafPositions[0, bi];
                    }
                    else
                    {
                        leafPositionsHalcyon[0, j] = leafPositions[0, ai];
                    }

                    if (leafPositions[1, ai] < leafPositions[1, bi])
                    {
                        leafPositionsHalcyon[1, j] = leafPositions[1, ai];
                    }
                    else
                    {
                        leafPositionsHalcyon[1, j] = leafPositions[1, bi];
                    }
                }

                double[,] leafPositionsHalcyon2 = new double[2, 56];

                for (int j = 0; j < 56; j++)
                {
                    // Leaf positions are given from bottom to top by ESAPI,
                    // but the Aperture class expects them from top to bottom
                    int indj = 55 - j;
                    if (leafPositionsHalcyon[0, indj] > leafPositionsHalcyon[1, indj])
                    {
                        leafPositionsHalcyon2[0, j] = 0;
                        leafPositionsHalcyon2[1, j] = 0;
                    }
                    else
                    {
                        leafPositionsHalcyon2[0, j] = leafPositionsHalcyon[0, indj];
                        leafPositionsHalcyon2[1, j] = leafPositionsHalcyon[1, indj];
                    }                 
                }
                return leafPositionsHalcyon2;
            }
        }

        public double[] GetLeafWidths(Patient patient, PlanSetup plan, Beam beam)
        {
            try
            {
                double [] MLC120 = new double[] {14, 10, 10, 10, 10, 10, 10, 10, 10, 10,  5,  5, 
                                                5,  5,  5,  5,  5, 5,  5,  5,  5,  5,  5,  5,  5,
                                                5,  5,  5,  5,  5,  5,  5,  5,  5, 5,  5,  5,  5,
                                                5,  5,  5,  5,  5,  5,  5,  5,  5,  5,  5,  5, 10,
                                                10, 10, 10, 10, 10, 10, 10, 10, 14};
                
                double [] MLC120HD = new double[] {5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 2.5, 2.5,
                                                2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5,
                                                    2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5,
                                                    2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5, 2.5,
                                                    5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
                
                double[] MLCHalcyon = new double[] {5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                                                    5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                                                    5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                                                    5, 5, 5, 5, 5};

                List<string> MLC120Machines = new List<string> {"TrueBeamSN4831","TRILOGY6085", "TR-SN5387"};
                List<string> MLC120HDMachines = new List<string> {"Linac3", "Linac4"};
                List<string> MLCHalcyonMachines = new List<string> { "HalcyonSN1386" };

                double [] leafwidths = new double[] { };
                
                var machine = beam.TreatmentUnit.Id;
                leafwidths = MLC120;
                
                if (MLC120HDMachines.Contains(machine))
                {
                    leafwidths = MLC120HD;
                }
                else if (MLCHalcyonMachines.Contains(machine))
                {
                    leafwidths = MLCHalcyon;
                }
                return leafwidths.ToArray();
            }
            catch (Exception e)
            {
                throw new LeafWidthsNotFoundException
                    ("Unable to obtain leaf widths for beam " + beam.Id, e);
            }
        }
/*
        public double[,] GetLeafPositions(ControlPoint controlPoint)
        {
            int m = controlPoint.LeafPositions.GetLength(0);
            int n = controlPoint.LeafPositions.GetLength(1);

            double[,] leafPositions = new double[m, n];

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    // Leaf positions are given from bottom to top by ESAPI,
                    // but the Aperture class expects them from top to bottom
                    leafPositions[i, j] = controlPoint.LeafPositions[i, n - j - 1];

            return leafPositions;
        }

        public double[] GetLeafWidths(Patient patient, PlanSetup plan, Beam beam)
        {
            try
            {
                return GetLeafWidthsFromAria(patient, plan, beam).ToArray();
            }
            catch (Exception e)
            {
                throw new LeafWidthsNotFoundException
                    ("Unable to obtain leaf widths for beam " + beam.Id, e);
            }
        }

        private IEnumerable<double> GetLeafWidthsFromAria(
            Patient patient, PlanSetup plan, Beam beam)
        {
            var course = plan.Course;

            using (var ac = new AriaContext())
            {
                // Use ESAPI IDs to get to the ARIA Radiation row
                var dbPatient = ac.Patients.First(p => p.PatientId == patient.Id);
                var dbCourse = dbPatient.Courses.First(c => c.CourseId == course.Id);
                var dbPlan = dbCourse.PlanSetups.First(ps => ps.PlanSetupId == plan.Id);
                var dbRad = dbPlan.Radiations.First(r => r.RadiationId == beam.Id);

                // Use the RadiationSer to get to the MLC add-on
                var dbFieldAddOns = ac.FieldAddOns.Where(f => f.RadiationSer == dbRad.RadiationSer);
                var dbMlcAddOn = dbFieldAddOns.First(f => f.AddOn.AddOnType == "MLC");

                // Use the MLC row to get to the leaves (and their width)
                // Note: We only need to use one of the MLC banks because
                // the leaf widths are the same between leaf pairs
                var dbMlc = dbMlcAddOn.AddOn.MLC;
                var dbMlcLeaves = dbMlc.MLCBanks.First().MLCLeaves;
                return dbMlcLeaves.Select(lf => lf.Width);
            }
        } */

    }
}
