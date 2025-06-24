# Repository Instructions

When making changes to this Unity project:

- Consult the official Unity documentation at <https://docs.unity3d.com> to confirm APIs and best practices.
- Ensure compatibility with **Unity 6000.1.6f1**, the version used by Timeless Echoes.
- Do not create or commit `.meta` files.
- Avoid using obsolete Unity API calls. For example, replace `Object.FindObjectOfType` with `Object.FindFirstObjectByType` or `Object.FindAnyObjectByType`.
- Note the warning `CS0618: 'CinemachineVirtualCamera' is obsolete`. Use `CinemachineCamera` instead of the deprecated `CinemachineVirtualCamera`.
