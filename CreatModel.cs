using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatModel
{
    public class CreatModel
    {
        [Transaction(TransactionMode.Manual)]
        public class CreationModel : IExternalCommand
        {
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                //Document doc = commandData.Application.ActiveUIDocument.Document;
                //var res1 = new FilteredElementCollector(doc)
                //    .OfClass(typeof(WallType))
                //    //.Cast<Wall>()
                //    .OfType<WallType>()
                //    .ToList();

                //var res2 = new FilteredElementCollector(doc)
                //    .OfClass(typeof(FamilyInstance))
                //    //.Cast<Wall>()
                //    .OfType<FamilyInstance>()
                //    .ToList();

                //return Result.Succeeded;

                Document doc = commandData.Application.ActiveUIDocument.Document;

                Level level1, level2;
                TakeLevels(doc, out level1, out level2);

                CreateWalls(doc, level1, level2);

                return Result.Succeeded;
            }

            private static void CreateWalls(Document doc, Level level1, Level level2)
            {
                double width = UnitUtils.ConvertToInternalUnits(10000, DisplayUnitType.DUT_MILLIMETERS);
                double depth = UnitUtils.ConvertToInternalUnits(5000, DisplayUnitType.DUT_MILLIMETERS);
                double dx = width / 2;
                double dy = depth / 2;

                List<XYZ> points = new List<XYZ>();
                points.Add(new XYZ(-dx, -dy, 0));
                points.Add(new XYZ(dx, -dy, 0));
                points.Add(new XYZ(dx, dy, 0));
                points.Add(new XYZ(-dx, dy, 0));
                points.Add(new XYZ(-dx, -dy, 0));

                List<Wall> walls = new List<Wall>();

                Transaction ts = new Transaction(doc, "Создание стены");
                ts.Start();
                for (int i = 0; i < 4; i++)
                {
                    Line line = Line.CreateBound(points[i], points[i + 1]);
                    Wall wall = Wall.Create(doc, line, level1.Id, false);
                    walls.Add(wall);
                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                }
                CreateDoor(doc, level1, walls[0]);
                CreateWindow(doc, level1, walls[1]);
                CreateWindow(doc, level1, walls[2]);
                CreateWindow(doc, level1, walls[3]);

                ts.Commit();
            }

            private static void CreateWindow(Document doc, Level level1, Wall wall)
            {
                FamilySymbol winType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("1000 x 1800 мм"))
                    .Where(x => x.FamilyName.Equals("Стандартные"))
                    .FirstOrDefault();

                LocationCurve hostCurve = wall.Location as LocationCurve;
                XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                XYZ point = (point1 + point2) / 2;

                if (!winType.IsActive)
                    winType.Activate();

                var window = doc.Create.NewFamilyInstance(point, winType, wall, level1, StructuralType.NonStructural);
                Parameter sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                double sh = UnitUtils.ConvertToInternalUnits(900, DisplayUnitType.DUT_MILLIMETERS);
                sillHeight.Set(sh);
            }

            private static void CreateDoor(Document doc, Level level1, Wall wall)
            {
                FamilySymbol doorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("1000 x 2100 мм"))
                    .Where(x => x.FamilyName.Equals("Межкомнатные"))
                    .FirstOrDefault();

                LocationCurve hostCurve = wall.Location as LocationCurve;
                XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                XYZ point = (point1 + point2) / 2;

                if (!doorType.IsActive)
                    doorType.Activate();

                doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
            }


            private static void TakeLevels(Document doc, out Level level1, out Level level2)
            {
                List<Level> listLevel = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .OfType<Level>()
                    .ToList();

                level1 = listLevel
                    .Where(x => x.Name.Equals("1-ый этаж"))
                    .FirstOrDefault();
                level2 = listLevel
                    .Where(x => x.Name.Equals("2-ой этаж"))
                    .FirstOrDefault();

            }
        }
    }
}
