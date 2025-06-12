# GitOps
This repo contains all Kubernetes manifests organized for GitOps with Argo CD and Kustomize.

## 📁 Repository Layout

- **`apps/`**  
  Contains Argo CD Application manifests (an “App-of-Apps”), one `*-app.yaml` per service. Argo CD points here to bootstrap every microservice Application.

- **`base/`**  
  Holds the core, environment-agnostic Kustomize overlays for each component: namespaces, Deployments, Services, Secrets, etc. No environment-specific values here.

- **`overlays/`**  
  Contains environment-specific patches (e.g. `production/…`) that overlay onto the corresponding `base/` folders to customize resource limits, secrets, or other config per environment.

## 🚀 Getting Started
### ➕ Adding a New Service
1. Create a “base” overlay under **`base/<service>/`** :
   - **`kustomization.yaml`**
   - **`namespace.yaml`**
   - **`deployment.yaml`** (and **`service.yaml`**, etc.)

2. Add an Argo CD Application in apps/ named **`<service>-app.yaml`**:

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: <service>-app
  namespace: argocd
spec:
  source:
    repoURL: "https://gitlab.au.dk/swwao/f2025/exams-projects/Group-7/gitops.git"
    path: base/<service>
    targetRevision: main
  destination:
    server: https://kubernetes.default.svc
    namespace: argocd
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
```

Update the App-of-Apps by adding your <service>-app.yaml to apps/kustomization.yaml.

Commit & push.

### 🎨 Using Overlays
To override values for an environment (e.g. production):

Create **`overlays/production/<service>/kustomization.yaml`** that bases on **`../../../base/<service>`** and lists your patches.

In your Argo CD Application (**`apps/<service>/`**) set:

````yaml
spec:
  source:
    path: overlays/production/<service>
````

Commit & push.

