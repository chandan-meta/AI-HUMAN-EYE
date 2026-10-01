using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace VisionLab.Editor {
    public class AssetAudit : IPreprocessBuildWithReport {
        public int callbackOrder=>0;
        [MenuItem("Vision Lab/Audit assets")]
        public static void Run(){
            var rows=new[]{("Eye","Zip/Eye_Hero.fbx","FOUND (zip)"),("Human skin","Zip/Extra/FitnessMan.fbx","FOUND (zip)"),("Muscles","Anatomy/Muscles.fbx","SOURCED — Z-Anatomy / BodyParts3D"),("Skull","Anatomy/Skull.fbx","SOURCED — Z-Anatomy / BodyParts3D"),("Brain","Anatomy/Brain.fbx","SOURCED — Z-Anatomy / BodyParts3D"),("Niko","Niko/Robot.fbx","SOURCED — Quaternius CC0"),("Convex lens","Zip/Extra/ConvexLens.fbx","FOUND (zip)"),("Concave lens","Zip/Extra/Concave_Lens.fbx","FOUND (zip)"),("Rods and cones","Zip/A My Work/ConesRod/Rod_and_Cones.fbx","FOUND (zip)"),("Occipital lobe","Anatomy/OccipitalLobe.fbx","SOURCED — Z-Anatomy / BodyParts3D")};
            var text="# Scene 2 asset audit\n\n| Role | Status | File |\n|---|---|---|\n";
            foreach(var row in rows)text+="|"+row.Item1+"|"+(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/SourceAssets/"+row.Item2)?row.Item3:"MISSING")+"|"+row.Item2+"|\n";
            text+="\nFull archive inventory: ZIP_INVENTORY.json (includes caches, marked project_asset=false).\n\nGenerated environment, scan plane, and beam are custom mesh assets. TTS and generated tones are development audio. Observatory dome, instrument variety, final gestures and recorded VO remain production gates. See ACCEPTANCE.md.\n";
            File.WriteAllText("Assets/_Project/Docs/ASSET_AUDIT.md",text);AssetDatabase.Refresh();
        }
        public void OnPreprocessBuild(BuildReport report){
            if((report.summary.options & BuildOptions.Development)==0)
                throw new BuildFailedException("Scene 2 production acceptance is pending: final VO, anatomy alignment review, gesture authoring and physical-device/performance validation. Development builds are available.");
        }
    }
}
