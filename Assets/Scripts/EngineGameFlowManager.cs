using System.Collections.Generic;
using UnityEngine;

public class EngineGameFlowManager : MonoBehaviour
{
    public EngineAssemblyManager assemblyManager;

    [Header("Scene References")]
    public GameObject welcomeMenuRoot;
    public GameObject loosePartsRoot;
    public GameObject snapZonesRoot;

    [Header("Completion")]
    public List<string> sectionsToComplete = new List<string> { "Front", "Side_A", "Side_B", "Top" };
    public bool testRequiresPracticeFirst = true;

    private bool sessionRunning;
    private bool practiceCompleted;
    private bool buildCompleteHandled;

    private void Start()
    {
        if (welcomeMenuRoot != null)
            welcomeMenuRoot.SetActive(true);

        if (loosePartsRoot != null)
            loosePartsRoot.SetActive(false);

        if (snapZonesRoot != null)
            snapZonesRoot.SetActive(false);
    }

    private void Update()
    {
        if (!sessionRunning || assemblyManager == null)
            return;

        if (AreAllSectionsComplete())
            HandleBuildComplete();
    }

    public void StartPracticeMode()
    {
        BeginSession(EngineAssemblyManager.BuildMode.Practice);
    }

    public void StartTestMode()
    {
        if (testRequiresPracticeFirst && !practiceCompleted)
        {
            Debug.Log("Finish Practice Mode first.");
            return;
        }

        BeginSession(EngineAssemblyManager.BuildMode.Test);
    }

    private void BeginSession(EngineAssemblyManager.BuildMode mode)
    {
        if (assemblyManager == null)
            return;

        sessionRunning = true;
        buildCompleteHandled = false;

        if (welcomeMenuRoot != null)
            welcomeMenuRoot.SetActive(false);

        if (loosePartsRoot != null)
            loosePartsRoot.SetActive(true);

        if (snapZonesRoot != null)
            snapZonesRoot.SetActive(true);

        assemblyManager.SetBuildMode(mode);
        assemblyManager.ResetBuild();
    }

    private bool AreAllSectionsComplete()
    {
        if (sectionsToComplete == null || sectionsToComplete.Count == 0)
            return false;

        foreach (string sectionName in sectionsToComplete)
        {
            if (!assemblyManager.IsSectionComplete(sectionName))
                return false;
        }

        return true;
    }

    private void HandleBuildComplete()
    {
        if (buildCompleteHandled)
            return;

        buildCompleteHandled = true;
        sessionRunning = false;

        if (assemblyManager.CurrentMode == EngineAssemblyManager.BuildMode.Practice)
        {
            practiceCompleted = true;
            Debug.Log("Practice complete. Test Mode is now available.");
        }
        else
        {
            Debug.Log("Test complete.");
        }

        if (welcomeMenuRoot != null)
            welcomeMenuRoot.SetActive(true);
    }
}