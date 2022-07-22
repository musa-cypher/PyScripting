using System;
using System.Collections.Generic;
using System.Text;

namespace PyScripting.ScriptingEngine
{
    public class InitialiserCode
    {
        public static string GetInitialiserSource()
        {
            var pysrc =

            "from clr_loader import get_coreclr\n" +                          // 1
            "from pythonnet import set_runtime\n" +                           // 2
            "import sys\n" +                                                  // 3
            "sys.path.append(r\'C:\\Users\\musa.mohamma\\Documents\\R&D\\smbs_scripting\\smbs-be\\SMBS.Server\\SMBS.Shared\\bin\\Debug\\netcoreapp3.1\')\n" +
            "sys.path.append(r\'C:\\Users\\musa.mohamma\\Documents\\R&D\\smbs_scripting\\smbs-be\\SMBS.Server\\SMBS.Engine\\bin\\Debug\\netcoreapp3.1\')\n" +
            "set_runtime(get_coreclr(r\'C:\\Users\\musa.mohamma\\Documents\\R&D\\smbs_scripting\\smbs-be\\SMBS.Server\\SMBS.Shared\\bin\\Debug\\netcoreapp3.1\\SMBS.Shared.runtimeconfig.json\'))\n" +
            "import clr\n" +                                                   // 7
            "clr.AddReference(\'SMBS.Shared\')\n" +                            // 8
            "clr.AddReference(\'SMBS.Engine\')\n" +                            // 9
            "import System\n" +                                                // 10



            "from SMBS.Shared.DataImport import (SingleInput, PvtData, " +      // 11
            "PvtDataRow, LabPVT, RockData, TankData)\n" +                       // 12

            "from SMBS.Shared.DataImport import (AquiferData, " +               // 13
            "RelativePermeabilityData)\n" +                                     // 14

            "from SMBS.Shared.DataImport import RelativePermeabilityDataRow\n" +         // 15
            "from SMBS.Shared.DataImport import ProductionData, ProductionDataRow\n" +   // 16
            "from SMBS.Shared import *\n" +                                              // 17
            "from SMBS.Engine import SetUp\n" +                                          // 18


            "from openpyxl import Workbook, load_workbook\n" +                           // 19



            "def convert_python_datetime_to_csharp_datetime(python_datetime):\n" +       // 20
            "    d = python_datetime\n" +                                                // 21
            "    return System.DateTime(d.year, d.month, d.day, d.hour, d.minute, d.second)\n" +//22

            "def to_csharp_int_list(lst):\n" +                                           // 23
            "    lstcs = System.Collections.Generic.List[System.Int32]()\n" +            // 24
            "    for e in lst:\n" +                                                      // 25
            "        lstcs.Add(e)\n" +                                                   // 26
            "    return lstcs\n" +                                                       // 27


            "# Tank Data\n" +                                                            // 28
            "def _read_tank_data(ws):\n" +                                               // 29
            "    convert = convert_python_datetime_to_csharp_datetime\n" +               // 30
            "    tnk = TankData()\n" +                                                   // 31
            "    tnk.StartDateOfProduction = convert(ws['B2'].value)\n" +                // 32
            "    tnk.FluidType = ws['B3'].value\n" +                                     // 33
            "    tnk.InitialPressure = ws['B4'].value\n" +                               // 34
            "    tnk.ConnateWaterSaturation = ws['B5'].value\n" +                        // 35

            "    props = [\'GasCap\', \'StandardOilInitiallyInPlace\', " +
            "             \'StandardGasInitiallyInPlace\'," +
            "             \'Thickness\', \'Length\', \'Width\', \'Radius\']\n" +         // 36

            "    rows_gen = ws.iter_rows(min_row = 7, min_col = 2, " +
            "    max_col = 3, max_row = 13, values_only = True)\n" +                     // 37

            "    for prop, row in zip(props, rows_gen):\n" +                             // 38
            "        low, up = row\n" +                                                  // 39
            "        setattr(tnk, prop + \'LowerBound\', low)\n" +                       // 40
            "        setattr(tnk, prop + \'UpperBound\', up)\n" +                        // 41


            "    # tank production data\n" +                                             // 42
            "    min_row, max_row = 4, 11\n" +                                           // 43
            "    min_col, max_col = 7, 13\n" +                                           // 44

            "    props = [\"Time\", \"Pressure\", \"OilProduced\", " +
            "             \"GasProduced\", \"WaterProduced\", " +
            "             \"GasInjected\", \"WaterInjected\"]\n" +                       // 45

            "    rows_gen = ws.iter_rows(min_row = min_row, min_col = min_col, " +
            "                            max_col = max_col, max_row = max_row, " +
            "                            values_only = True)\n" +                        // 46
            "    prod_data = ProductionData()\n" +                                       // 47
            "    for row in rows_gen:\n" +                                               // 48
            "        row_data = ProductionDataRow()\n" +                                 // 49
            "        for prop, col in zip(props, row):\n" +                              // 50
            "            if prop == \"Time\":\n" +                                       // 51
            "                setattr(row_data, prop, " +
            "                  convert_python_datetime_to_csharp_datetime(col))\n" +     // 52
            "            else:\n" +                                                      // 53
            "                setattr(row_data, prop, col)\n" +                           // 54
            "        prod_data.Add(row_data)\n" +                                        // 55

            "    tnk.ProductionData = prod_data\n" +                                     // 56


            "    # Tank data Lab PVT Data\n" +                                           // 57
            "    pvt = LabPVT()\n" +                                                     // 58
            "    SurfacePVT = PvtDataRow()      #SurfacePVT()\n" +                       // 59
            "    BubblePointPVT = PvtDataRow()  #BubblePointPVT()\n" +                   // 60
            "    DewPointPVT = PvtDataRow()     #DewPointPVT()\n" +                      // 61

            "    objs = [SurfacePVT, BubblePointPVT, DewPointPVT]\n" +                   // 62
            "    col_gen = ws.iter_cols(min_row = 39, min_col = 2, " +
            "    max_col = 4, max_row = 43, values_only = True)\n" +                     // 63
            "    for obj, col in zip(objs, col_gen):\n" +                                // 64
            "        obj.Pressure, obj.Temperature, obj.GasOilRatio, obj.OilDensity, obj.GasDensity = col\n" + // 65

            "    pvt.SurfacePVT = SurfacePVT\n" +                                        // 65
            "    pvt.BubblePointPVT = BubblePointPVT\n" +                                // 66
            "    pvt.DewPointPVT = DewPointPVT\n" +                                      // 67

            "    tnk.LabPVTMeasurement = pvt\n" +                                        // 68


            "    return tnk\n" +                                                         // 69


            "# Rock Data\n" +                                                           // 70
            "def _read_rock_data(ws):\n" +                                              // 80
            "    rows_gen = ws.iter_rows(min_row = 28, min_col = 2, max_col = 3, " +
            "                            max_row = 29, values_only = True)\n" +         // 81
            "    rows = tuple(rows_gen)\n" +                                            // 82
            "    rock_data = RockData()\n" +                                            // 83
            "    rock_data.PermeabilityLowerBound, rock_data.PermeabilityUpperBound = rows[0]\n" + // 84
            "    rock_data.PorosityLowerBound, rock_data.PorosityUpperBound = rows[1]\n" +  // 85
            "    return rock_data\n" +                                                  // 84


            "# Aquifer Data\n" +                                                        // 85
            "def _read_aquifer_data(ws):\n" +                                           // 86
            "    aq = AquiferData()\n" +                                                // 87
            "    aq.Position = ws.cell(16, 2).value\n" +                                // 88
            "    aq.Geometry = ws.cell(17, 2).value\n" +                                // 89
            "    aq.ExternalBoundaryCondition = ws.cell(18, 2).value\n" +               // 90
            "    aq.WaterInfluxModel = ws.cell(19, 2).value\n" +                        // 91

            "    props = [\"OuterInnerRadius\", \"EncroachmentAngle\", \"Volume\", \"Anisotropy\"]\n" + // 92

            "    rows_gen = ws.iter_rows(min_row = 21, min_col = 2, max_col = 3, " +
            "                            max_row = 24, values_only = True)\n" +         // 93
            "    for prop, row in zip(props, rows_gen):\n" +                            // 94
            "        low, up = row\n" +                                                 // 95
            "        setattr(aq, prop + 'LowerBound', low)\n" +                         // 96
            "        setattr(aq, prop + 'UpperBound', up)\n" +                          // 97


            "    return aq\n" +                                                         // 98


            "# RelativePermeabilityData\n" +                                            // 99
            "def _read_relperm_data(ws):\n" +                                           // 100
            "    relperm = RelativePermeabilityData()\n" +                              // 101
            "    props = [\"OilRelPerm\", \"WaterRelPerm\", \"GasRelPerm\"]\n" +        // 102

            "    rows_gen = ws.iter_rows(min_row = 33, min_col = 2, " +
            "                max_col = 4, max_row = 35, values_only = True)\n" +        // 103

            "    for prop, row in zip(props, rows_gen):\n" +                            // 104
            "        obj = RelativePermeabilityDataRow()\n" +                           // 105
            "        obj.ResidualSaturation, obj.EndPoint, obj.Exponent = row\n" +      // 106
            "        setattr(relperm, prop, obj)\n" +                                   // 107


            "    return relperm\n" +                                                    // 108


            "# PVT Data\n" +                                                            // 109
            "def _read_extpvt_data(ws):\n" +                                            // 110
            "    pvt_data = PvtData()\n" +                                              // 111
            "    min_row, max_row = 7, 357\n" +                                         // 112
            "    min_col, max_col = 1, 18\n" +                                          // 113

            "    for row in range(min_row, max_row + 1):\n" +                           // 114
            "        pvt_row = PvtDataRow()\n" +                                        // 115
            "        for col in range(min_col, max_col + 1):\n" +                       // 116
            "            header1 = ws.cell(3, col).value\n" +                           // 117
            "            header2 = ws.cell(4, col).value or \"\" \n" +                  // 118
            "            prop = (header1 + header2).replace(\" \", \"\")\n" +           // 119
            "            if prop == \"WaterCompress.\": prop = \"WaterCompressibility\"\n" +   // 120
            "            if not hasattr(pvt_row, prop):\n" +                                   // 121
            "                raise ValueError(f\"header {header1 + header2} is invalid\")\n" + // 122
            "            setattr(pvt_row, prop, ws.cell(row, col).value)\n" +                  // 123
            "        pvt_data.Add(pvt_row)\n" +                                                // 124


            "    return pvt_data \n" +                                                         // 125


            "def create_single_input_from_excel(xlfile: str,\n" +                              // 126
            "                                   tanksheet: str = \"Tank\",\n" +                // 127
            "                                   pvtsheet: str = \"ExtPVT\") -> SingleInput:\n" + // 128
            "    wb = load_workbook(xlfile)\n" +                                               // 129
            "    tank_data = _read_tank_data(wb[tanksheet])\n" +                               // 130
            "    rock_data = _read_rock_data(wb[tanksheet])\n" +                               // 131
            "    aquifer_data = _read_aquifer_data(wb[tanksheet])\n" +                         // 132
            "    relperm_data = _read_relperm_data(wb[tanksheet])\n" +                         // 133
            "    extpvt_data = _read_extpvt_data(wb[pvtsheet])\n" +                            // 134

            "    inp = SingleInput()\n" +                                                      // 135
            "    inp.TankData = tank_data\n" +                                                 // 136
            "    inp.RockData = rock_data\n" +                                                 // 137
            "    inp.AquiferData = aquifer_data\n" +                                           // 138
            "    inp.RelativePermeabilityData = relperm_data\n" +                              // 139
            "    inp.PvtData = extpvt_data\n" +                                                // 140

            "    return inp\n";                                                                // 141

            return pysrc;

        }

        public static string GetOutputRedirectorSource()
        {
            string pysrc =
                "import sys\n" +
                "class OutputRedirect :\n" +

                "    def beginTrappingOutput(self):\n" +
                "        self.outputBuffer = []\n" +
                "        self.old_output = sys.stdout\n" +
                "        sys.stdout = self\n" +

                "    def write(self, expr):\n" +
                "        \"\"\" this is an internal utility used to trap the output.\n" +
                "        add it to a list of strings - this is more efficient\n" +
                "        than adding to a possibly very long string.\"\"\"\n" +
                "        self.outputBuffer.append(str(expr))\n" +


                "    def getStandardOutput(self):\n" +
                "        \"Hand over output so far, and empty the buffer\"\n" +
                "        text = \'\'.join(self.outputBuffer)\n" +
                "        self.outputBuffer = []\n" +
                "        return text\n" +

                "    def endTrappingOutput(self) :\n" +
                "        sys.stdout = self.old_output\n" +
                "        # return any more output\n" +
                "        return self.getStandardOutput()\n" +

                "Redirector = OutputRedirect()\n";

            return pysrc;
        }
    }
}
