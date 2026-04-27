using System;
using System.Collections.Generic;
using UnityEngine;

public class EngineAssemblyManager : MonoBehaviour
{
    public enum BuildMode
    {
        Practice,
        Test
    }

    [Serializable]
    public class PartStep
    {
        public string partId;
        public AssemblyPart part;
        public SnapZone zone;
        public bool availableFromStart;
        public string[] requiredPartIds;
    }

    [Serializable]
    public class BuildSection
    {
        public string sectionName;
        public bool unlockedAtStart = true;
        public List<PartStep> steps = new List<PartStep>();
    }

    [Header("Mode")]
    public BuildMode currentMode = BuildMode.Practice;

    [Header("Sections")]
    public List<BuildSection> sections = new List<BuildSection>();
    public string activeSectionName = "Front";
    public bool onlyActiveSectionCanBeUsed = true;

    [Header("Locking")]
    public bool disableGrabForLockedParts = true;

    [Header("Arrow Indicators (Practice Mode)")]
    [Tooltip("Prefab with PartArrowIndicator on it. One instance is spawned per part.")]
    public PartArrowIndicator arrowPrefab;

    [Tooltip("Optional parent for spawned arrows. If null, the manager is used.")]
    public Transform arrowsParent;

    private Dictionary<string, PartStep> stepLookup = new Dictionary<string, PartStep>();
    private Dictionary<string, string> partToSectionLookup = new Dictionary<string, string>();
    private Dictionary<string, PartArrowIndicator> arrowLookup = new Dictionary<string, PartArrowIndicator>();
    private HashSet<string> installedPartIds = new HashSet<string>();
    private HashSet<string> unlockedSections = new HashSet<string>();

    private bool sessionActive;

    public BuildMode CurrentMode => currentMode;
    public bool SessionActive => sessionActive;

    private void Awake()
    {
        BuildLookups();
    }

    private void Start()
    {
        ResetBuild();
    }

    private void Update()
    {
        UpdateGhosts();
        UpdateArrows();
    }

    public void StartSession()
    {
        sessionActive = true;
    }

    public void EndSession()
    {
        sessionActive = false;
        HideAllArrows();
    }

    private void BuildLookups()
    {
        stepLookup.Clear();
        partToSectionLookup.Clear();
        unlockedSections.Clear();

        foreach (BuildSection section in sections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.sectionName))
                continue;

            if (section.unlockedAtStart)
                unlockedSections.Add(section.sectionName);

            if (section.steps == null)
                continue;

            foreach (PartStep step in section.steps)
            {
                if (step == null)
                    continue;

                if (string.IsNullOrWhiteSpace(step.partId))
                {
                    if (step.part != null && !string.IsNullOrWhiteSpace(step.part.partId))
                        step.partId = step.part.partId;
                    else if (step.zone != null && !string.IsNullOrWhiteSpace(step.zone.partId))
                        step.partId = step.zone.partId;
                }

                if (string.IsNullOrWhiteSpace(step.partId))
                {
                    Debug.LogWarning("A step is missing a partId.", this);
                    continue;
                }

                if (!stepLookup.ContainsKey(step.partId))
                {
                    stepLookup.Add(step.partId, step);
                    partToSectionLookup.Add(step.partId, section.sectionName);
                }
                else
                {
                    Debug.LogWarning("Duplicate partId found: " + step.partId, this);
                }

                if (step.part != null)
                    step.part.partId = step.partId;

                if (step.zone != null)
                {
                    step.zone.partId = step.partId;
                    step.zone.Setup(this);
                }

                SpawnArrowFor(step);
            }
        }
    }

    private void SpawnArrowFor(PartStep step)
    {
        if (arrowPrefab == null)
            return;

        if (step.part == null)
            return;

        if (arrowLookup.ContainsKey(step.partId))
            return;

        Transform parent = arrowsParent != null ? arrowsParent : transform;
        PartArrowIndicator arrow = Instantiate(arrowPrefab, parent);
        arrow.name = "Arrow_" + step.partId;
        arrow.SetTarget(step.part.transform);
        arrow.SetVisible(false);
        arrowLookup.Add(step.partId, arrow);
    }

    private void UpdateGhosts()
    {
        foreach (BuildSection section in sections)
        {
            if (section == null || section.steps == null)
                continue;

            foreach (PartStep step in section.steps)
            {
                if (step == null || step.part == null || step.zone == null)
                    continue;

                bool showGhost =
                    currentMode == BuildMode.Practice &&
                    !IsInstalled(step.partId) &&
                    IsUnlocked(step.partId) &&
                    step.part.IsHeld;

                step.zone.SetGhostVisible(showGhost);
            }
        }
    }

    private void UpdateArrows()
    {
        if (arrowLookup.Count == 0)
            return;

        bool active = sessionActive && currentMode == BuildMode.Practice;

        foreach (BuildSection section in sections)
        {
            if (section == null || section.steps == null)
                continue;

            foreach (PartStep step in section.steps)
            {
                if (step == null || string.IsNullOrWhiteSpace(step.partId))
                    continue;

                if (!arrowLookup.TryGetValue(step.partId, out PartArrowIndicator arrow) || arrow == null)
                    continue;

                bool show =
                    active &&
                    step.part != null &&
                    !IsInstalled(step.partId) &&
                    IsUnlocked(step.partId) &&
                    !step.part.IsHeld;

                arrow.SetVisible(show);
            }
        }
    }

    public void SetBuildMode(BuildMode newMode)
    {
        currentMode = newMode;
        HideAllGhosts();
        HideAllArrows();
        RefreshAvailability();
    }

    public void SetPracticeMode()
    {
        SetBuildMode(BuildMode.Practice);
    }

    public void SetTestMode()
    {
        SetBuildMode(BuildMode.Test);
    }

    public void ResetBuild()
    {
        installedPartIds.Clear();

        foreach (BuildSection section in sections)
        {
            if (section == null || section.steps == null)
                continue;

            foreach (PartStep step in section.steps)
            {
                if (step?.part != null)
                    step.part.ResetPart();
            }
        }

        HideAllGhosts();
        HideAllArrows();
        RefreshAvailability();
    }

    private void HideAllGhosts()
    {
        foreach (BuildSection section in sections)
        {
            if (section == null || section.steps == null)
                continue;

            foreach (PartStep step in section.steps)
            {
                if (step?.zone != null)
                    step.zone.SetGhostVisible(false);
            }
        }
    }

    private void HideAllArrows()
    {
        foreach (KeyValuePair<string, PartArrowIndicator> kvp in arrowLookup)
        {
            if (kvp.Value != null)
                kvp.Value.SetVisible(false);
        }
    }

    public bool CanSnapPartToZone(AssemblyPart part, SnapZone zone)
    {
        if (part == null || zone == null)
            return false;

        if (part.partId != zone.partId)
            return false;

        if (!stepLookup.TryGetValue(part.partId, out PartStep step))
            return false;

        if (step.part != part || step.zone != zone)
            return false;

        if (IsInstalled(step.partId))
            return false;

        if (!IsUnlocked(step.partId))
            return false;

        return true;
    }

    public void TrySnapPart(AssemblyPart part, SnapZone zone)
    {
        if (!CanSnapPartToZone(part, zone))
            return;

        Transform target = zone.snapPoint != null ? zone.snapPoint : zone.transform;

        part.SnapTo(target);
        installedPartIds.Add(part.partId);
        zone.SetGhostVisible(false);

        if (arrowLookup.TryGetValue(part.partId, out PartArrowIndicator arrow) && arrow != null)
            arrow.SetVisible(false);

        Debug.Log(part.partId + " installed");

        RefreshAvailability();
    }

    public bool IsInstalled(string partId)
    {
        return installedPartIds.Contains(partId);
    }

    public bool IsUnlocked(string partId)
    {
        if (!stepLookup.TryGetValue(partId, out PartStep step))
            return false;

        if (IsInstalled(partId))
            return false;

        if (!IsPartInUsableSection(partId))
            return false;

        if (step.availableFromStart)
            return true;

        if (step.requiredPartIds == null || step.requiredPartIds.Length == 0)
            return false;

        foreach (string requiredId in step.requiredPartIds)
        {
            if (string.IsNullOrWhiteSpace(requiredId))
                continue;

            if (!installedPartIds.Contains(requiredId))
                return false;
        }

        return true;
    }

    private bool IsPartInUsableSection(string partId)
    {
        if (!partToSectionLookup.TryGetValue(partId, out string sectionName))
            return false;

        if (!unlockedSections.Contains(sectionName))
            return false;

        if (onlyActiveSectionCanBeUsed &&
            !string.Equals(sectionName, activeSectionName, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private void RefreshAvailability()
    {
        foreach (BuildSection section in sections)
        {
            if (section == null || section.steps == null)
                continue;

            foreach (PartStep step in section.steps)
            {
                if (step == null)
                    continue;

                if (step.zone != null)
                    step.zone.SetGhostVisible(false);

                if (step.part == null || step.part.IsSnapped)
                    continue;

                if (disableGrabForLockedParts)
                    step.part.SetInteractable(IsUnlocked(step.partId));
                else
                    step.part.SetInteractable(true);
            }
        }
    }

    public void SetActiveSection(string newSectionName)
    {
        activeSectionName = newSectionName;
        RefreshAvailability();
    }

    public void UnlockSection(string sectionName)
    {
        if (!string.IsNullOrWhiteSpace(sectionName))
        {
            unlockedSections.Add(sectionName);
            RefreshAvailability();
        }
    }

    public bool IsSectionComplete(string sectionName)
    {
        BuildSection section = sections.Find(s => s != null && s.sectionName == sectionName);
        if (section == null || section.steps == null || section.steps.Count == 0)
            return false;

        foreach (PartStep step in section.steps)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.partId))
                continue;

            if (!installedPartIds.Contains(step.partId))
                return false;
        }

        return true;
    }
}