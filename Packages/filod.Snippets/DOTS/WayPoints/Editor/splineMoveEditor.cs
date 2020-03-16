/*  This file is part of the "Simple Waypoint System" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;

namespace SWS
{
    /// <summary>
    /// Custom inspector for splineMove.
    /// <summary>
    [CustomEditor(typeof(SplineMoveAuthoring))]
    public class splineMoveEditor : moveEditor
    {
        //called whenever the inspector gui gets rendered
        public override void OnInspectorGUI()
        {
            //this pulls the relative variables from unity runtime and stores them in the object
            m_Object.Update();

            //draw custom variable properties by using serialized properties
            EditorGUILayout.PropertyField(m_Object.FindProperty("pathContainer"));

            EditorGUILayout.PropertyField(m_Object.FindProperty("startPoint"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("speed"));

            SerializedProperty loopType = m_Object.FindProperty("loopType");
            EditorGUILayout.PropertyField(loopType);
            if (loopType.enumValueIndex == 1)
                EditorGUILayout.PropertyField(m_Object.FindProperty("closedLoop"));
            else
                m_Object.FindProperty("closedLoop").boolValue = false;

            EditorGUILayout.PropertyField(m_Object.FindProperty("pathType"));

            EditorGUILayout.PropertyField(m_Object.FindProperty("waypointRotation"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("easeBetweenSection"));


            //we push our modified variables back to our serialized object
            m_Object.ApplyModifiedProperties();
        }


        //if this path is selected, display small info boxes above all waypoint positions
        void OnSceneGUI()
        {
            //get Path Manager component
            var path = GetPathTransform();

            //do not execute further code if we have no path defined
            if (path == null) return;

            //get waypoints array of Path Manager
            var waypoints = path.waypoints;
            Vector3 wpPos = Vector3.zero;
            float size = 1f;

            //loop through waypoint array
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (!waypoints[i]) continue;
                wpPos = waypoints[i].position;
                size = HandleUtility.GetHandleSize(wpPos) * 0.4f;

                //do not draw waypoint header if too far away
                if (size < 3f)
                {
                    //begin 2D GUI block
                    Handles.BeginGUI();
                    //translate waypoint vector3 position in world space into a position on the screen
                    var guiPoint = HandleUtility.WorldToGUIPoint(wpPos);
                    //create rectangle with that positions and do some offset
                    var rect = new Rect(guiPoint.x - 50.0f, guiPoint.y - 40, 100, 20);
                    //draw box at rect position with current waypoint name
                    GUI.Box(rect, waypoints[i].name);
                    Handles.EndGUI(); //end GUI block
                }
            }
        }
    }
}
