using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mingxu.Core.Models;
using Mingxu.Core.Rules;
using Mingxu.Core.Scoring;
using Newtonsoft.Json.Linq;

namespace Mingxu.Core.Parents
{
    public sealed class ParentCheckItem
    {
        public bool Ok { get; set; }
        public string Text { get; set; } = "";
    }

    /// <summary>PDF／報告用：父母姓名合參結構化內容（新生兒／改名共用）。</summary>
    public sealed class ParentReportBlock
    {
        public bool HasAnyParent { get; set; }
        public bool FatherFilled { get; set; }
        public bool MotherFilled { get; set; }
        public string StatusText { get; set; } = "";
        public string FatherSurname { get; set; } = "";
        public string FatherGiven { get; set; } = "";
        public string MotherSurname { get; set; } = "";
        public string MotherGiven { get; set; } = "";
        public string Grade { get; set; } = "";
        public List<ParentCheckItem> Checks { get; set; } = new List<ParentCheckItem>();
    }

    public static class ParentReportBuilder
    {
        public static ParentReportBlock Build(AnalysisRequest req, NameSuggestion sug)
        {
            var compounds = LoadCompoundSurnames();
            var fatherFull = Clean(req != null ? req.FatherName : "");
            var motherFull = Clean(req != null ? req.MotherName : "");
            var father = NameParse.SplitFullName(fatherFull, compounds);
            var mother = NameParse.SplitFullName(motherFull, compounds);
            var fatherFilled = fatherFull.Length > 0;
            var motherFilled = motherFull.Length > 0;
            var childGiven = sug != null ? (sug.Given ?? "") : "";

            var block = new ParentReportBlock
            {
                FatherFilled = fatherFilled,
                MotherFilled = motherFilled,
                HasAnyParent = fatherFilled || motherFilled,
                FatherSurname = fatherFilled ? (string.IsNullOrEmpty(father.Surname) ? "—" : father.Surname) : "未填寫",
                FatherGiven = fatherFilled ? (string.IsNullOrEmpty(father.Given) ? "—" : father.Given) : "未填寫",
                MotherSurname = motherFilled ? (string.IsNullOrEmpty(mother.Surname) ? "—" : mother.Surname) : "未填寫",
                MotherGiven = motherFilled ? (string.IsNullOrEmpty(mother.Given) ? "—" : mother.Given) : "未填寫",
            };

            if (!block.HasAnyParent)
            {
                block.StatusText = "未填父母姓名，本次略過家族合參。";
                return block;
            }

            if (fatherFilled && motherFilled)
                block.StatusText = "已納入父、母姓名合參。";
            else if (fatherFilled)
                block.StatusText = "已納入父親姓名合參（母親未填寫）。";
            else
                block.StatusText = "已納入母親姓名合參（父親未填寫）。";

            if (sug != null && sug.ParentUsed)
                block.Grade = NameScorer.GradeLabel(sug.ParentScore);

            // 1) 父母同字避諱
            var taboo = ParentTaboo.Extract(fatherFull, motherFull, compounds);
            var overlap = childGiven.Where(taboo.Contains).Select(c => c.ToString()).Distinct().ToList();
            if (overlap.Count == 0)
                block.Checks.Add(Ok("未使用父母姓名相同用字"));
            else
                block.Checks.Add(Fail("使用了與父母相同用字「" + string.Join("", overlap) + "」"));

            // 2) 指定避諱字
            var forbidden = NameParse.ParseForbidden(req != null ? req.ForbiddenChars : "");
            var forbiddenHits = new List<string>();
            foreach (var token in forbidden)
            {
                if (string.IsNullOrEmpty(token)) continue;
                if (childGiven.IndexOf(token, StringComparison.Ordinal) >= 0)
                    forbiddenHits.Add(token);
            }
            if (forbidden.Count == 0)
                block.Checks.Add(Ok("未使用指定避諱字"));
            else if (forbiddenHits.Count == 0)
                block.Checks.Add(Ok("未使用指定避諱字"));
            else
                block.Checks.Add(Fail("使用了指定避諱字「" + string.Join("、", forbiddenHits) + "」"));

            // 3) 輩分衝突（與父／母名字完全相同）
            var generationClash = false;
            var clashRoles = new List<string>();
            if (fatherFilled && !string.IsNullOrEmpty(father.Given) && father.Given == childGiven)
            {
                generationClash = true;
                clashRoles.Add("父親");
            }
            if (motherFilled && !string.IsNullOrEmpty(mother.Given) && mother.Given == childGiven)
            {
                generationClash = true;
                clashRoles.Add("母親");
            }
            if (!generationClash)
                block.Checks.Add(Ok("未產生明顯輩分衝突"));
            else
                block.Checks.Add(Fail("與" + string.Join("、", clashRoles) + "名字完全相同，存在輩分衝突"));

            return block;
        }

        private static ParentCheckItem Ok(string text)
        {
            return new ParentCheckItem { Ok = true, Text = text };
        }

        private static ParentCheckItem Fail(string text)
        {
            return new ParentCheckItem { Ok = false, Text = text };
        }

        private static string Clean(string text)
        {
            return (text ?? "").Trim().Replace(" ", "").Replace("　", "")
                .Replace("·", "").Replace("・", "").Replace(".", "").Replace("．", "");
        }

        private static IReadOnlyList<string> LoadCompoundSurnames()
        {
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (var i = 0; i < 10 && dir != null; i++)
                {
                    foreach (var rel in new[]
                    {
                        Path.Combine("_gen", "compound_surnames.json"),
                        Path.Combine("winforms", "_gen", "compound_surnames.json"),
                        "compound_surnames.json",
                    })
                    {
                        var path = Path.Combine(dir.FullName, rel);
                        if (!File.Exists(path)) continue;
                        var arr = JArray.Parse(File.ReadAllText(path));
                        return arr.Select(t => (t ?? "").ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    }
                    dir = dir.Parent;
                }
            }
            catch
            {
                // ignore — 單姓拆解仍可用
            }
            return new List<string>();
        }
    }
}
