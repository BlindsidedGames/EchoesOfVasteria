
# Starting "New" chats
- Look through relevant code, and derive as much information related to the topic as you can.
- Present your plan and wait for me to approve the plan before you make any changes.
- Ask clarifying questions.

# Repository Instructions

- Always use best practices and clean code when modifying or creating systems.
- Use `[SerializeField] private` for fields; prefer properties for public API
- Avoid `FindObjectOfType` & `GetComponent` in `Update`; cache in `Awake`

When making changes to this **2D** Unity project:

- Consult the official Unity documentation at <https://docs.unity3d.com> to confirm APIs and best practices.
- Ensure compatibility with **Unity 6000.2.1f1**, the version used by Echoes of Vasteria (Timeless Echoes).
- Do not create or commit `.meta` files unless absolutely required.
- Avoid using obsolete Unity API calls. For example, replace `Object.FindObjectOfType` with `Object.FindFirstObjectByType` or `Object.FindAnyObjectByType`.
- Note the warning `CS0618: 'CinemachineVirtualCamera' is obsolete`. Use `CinemachineCamera` instead of the deprecated `CinemachineVirtualCamera`.
- Do not modify `Assets/Scenes/Main.unity` unless explicitly instructed to do so.
- Unity tests cannot be run in this environment. Only add or modify Unity tests when explicitly requested; otherwise, skip them but run any tests you can on your changes.

## Task Suggestion Guidelines

- Default to proposing a single task that accomplishes the user's goal.
- When identifying bugs, refactorings, or performance issues, provide a separate task for each distinct item.
- If the user explicitly asks for a suggested task, limit the response to one task.
