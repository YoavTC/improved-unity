using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(Object), true)]
public class AutoComponentSelector : PropertyDrawer
{
    private const float BUTTON_WIDTH = 50f;
    private const string BUTTON_LABEL = "Set";
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Skip if the property is not an automatically assignable object reference or is already assigned
        bool isObject = typeof(Component).IsAssignableFrom(fieldInfo.FieldType);
        bool isReferenceField = property.propertyType == SerializedPropertyType.ObjectReference;
        bool isNull = property.objectReferenceValue == null;
        if (!isObject || !isReferenceField || !isNull)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        // Layout: field + button
        Rect fieldRect = new Rect(position.x, position.y, position.width - BUTTON_WIDTH - 2, position.height);
        Rect buttonRect = new Rect(position.x + position.width - BUTTON_WIDTH, position.y, BUTTON_WIDTH, position.height);
        EditorGUI.PropertyField(fieldRect, property, label);

        if (GUI.Button(buttonRect, BUTTON_LABEL))
        {
            Component gameObject = (Component) property.serializedObject.targetObject;
            Type fieldType = fieldInfo.FieldType;

            // Find and assign a matching component to the field
            Object matchingComponent = FindComponent(fieldType, gameObject.gameObject);
            if (matchingComponent != null)
            {
                AssignField(property, matchingComponent);
                return;
            }

            Debug.Log($"No component of type {fieldInfo.FieldType.Name} found!");
        }
    }

    private Object FindComponent(Type targetType, GameObject gameObject)
    {
        // If the target type is a GameObject, return the GameObject itself
        if (targetType.IsEquivalentTo(typeof(GameObject)))
            return gameObject.gameObject;
        
        // // Search the target object itself
        if (gameObject.TryGetComponent(targetType , out var component))
            return component;
        
        // Search children
        Component childComponent = gameObject.GetComponentInChildren(targetType, true);
        if (childComponent)
            return childComponent;
        
        // Search parents
        Component parentComponent = gameObject.GetComponentInParent(targetType, true);
        if (parentComponent)
            return parentComponent;
        
        // Search scene
        var sceneComponent = Object.FindFirstObjectByType(targetType);
        if (sceneComponent)
            return sceneComponent;

        return null;
    }

    private void AssignField(SerializedProperty property, Object value)
    {
        property.objectReferenceValue = value;
        property.serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"Assigned {value.GetType().Name} to {property.displayName}");
    }
}