# Immersive HMD Sample Project
360 Video & Ambix Spatial Audio for Unity

본 프로젝트는 실감형 콘텐츠 제작 교육을 목적으로 제작된
Unity HMD 기반 레퍼런스 프로젝트입니다.

360 영상과 Ambix(FOA) 공간 음향을
HMD 환경에서 적용하는 방법을
실제 동작하는 프로젝트 구조를 통해 제공합니다.

---

## 프로젝트 목적 / Project Purpose

[KR]
- HMD 환경에서 360 영상 콘텐츠를 적용하는 기본 구조 제공
- Ambix(1차 앰비소닉) 음원을 활용한 공간 음향 구성 방식 제시
- 이동 오브젝트에 대한 Spatial SFX 적용 구조 예제 제공
- 실감형 콘텐츠 제작자의 기술적 진입 장벽 완화

[EN]
- Provide a basic structure for applying 360° video content in HMD environments
- Demonstrate Ambix (First-Order Ambisonics) spatial audio implementation
- Present spatial SFX structures for moving objects
- Lower the technical entry barrier for immersive content creators

---

## 포함된 주요 내용 / Included Features

[KR]
- HMD용 360 영상 재생 씬
- Ambix(FOA) 배경 공간 음향 적용 예제
- 이동 오브젝트 기반 Spatial SFX 구조
- 샘플 Ambix 음원 및 SFX 리소스
- 즉시 실행 가능한 Unity 프로젝트 구조

[EN]
- 360° video playback scenes for HMD
- Ambix (FOA) spatial background audio examples
- Spatial SFX implementation for moving objects
- Sample Ambix audio and SFX assets
- Ready-to-run Unity project structure

※ 고해상도 360 영상 파일은 용량 문제로 인해 별도 제공됩니다.  
※ High-resolution 360° video files are provided separately due to file size limitations.

---

## 360 영상 처리 방식 안내 (중요)
## 360 Video Handling Notice (Important)

[KR]
본 프로젝트에서 360 영상 파일을
Unity 프로젝트의 Assets 폴더에 직접 포함할 경우,
빌드된 애플리케이션 실행이 정상적으로 이루어지지 않는 문제가 발생할 수 있습니다.

이에 따라 본 프로젝트는
런타임 중 360 영상 파일을 다운로드하여 사용하는 구조로 구성되어 있습니다.

기본 예제에서는 내부 네트워크(사설망) 환경을 기준으로
다운로드 기능이 구현되어 있으나,
해당 기능은 특정 서버나 네트워크 환경에 종속되지 않습니다.

사용자는 필요에 따라:
- 개인 서버
- 사내 서버
- 클라우드 스토리지 (예: Google Drive, AWS S3 등)

어떠한 환경으로든
영상 파일의 호스팅 위치를 자유롭게 변경하여 사용할 수 있습니다.

[EN]
Including large 360° video files directly inside the Unity Assets folder
may cause runtime issues or prevent the built application from launching properly.

For this reason, this project is designed to
download 360° video files at runtime instead of embedding them in the build.

The sample implementation uses an internal network environment,
but the download system itself is not tied to any specific server or network.

Users are free to host the video files on:
- Personal servers
- Internal company servers
- Cloud storage services (e.g., Google Drive, AWS S3, etc.)

and modify the download source as needed.

---

## 360 영상 다운로드 안내 / 360 Video Download Guide

[KR]
Unity Asset Store의 용량 제한으로 인해,
고해상도 360 영상 파일은 패키지에 포함되어 있지 않습니다.

아래 안내된 링크를 통해 영상 파일을 다운로드한 후,
런타임 다운로드 구조에 맞게 서버를 구성해 주세요.

[EN]
Due to Unity Asset Store file size limitations,
high-resolution 360° video files are not included in the package.

Please download the video files using the provided link
and host them according to the runtime download structure.

▶ Google Drive : https://drive.google.com/drive/folders/1QMtR6OuZ8Jln_p61UHv9UL--w2dVHqJ6?usp=drive_link

---

## 권장 사용 대상 / Recommended Users

[KR]
- 실감형 콘텐츠 / XR / VR 제작 입문자
- Unity에서 360 영상 및 공간 음향을 적용해보고자 하는 개발자
- Ambix(FOA) 공간 음향 구조를 학습하려는 사용자
- 교육, 연구, R&D 목적의 실감형 콘텐츠 제작자

[EN]
- Beginners in immersive content and XR/VR development
- Developers applying 360° video and spatial audio in Unity
- Users learning Ambix (FOA) spatial audio workflows
- Educators, researchers, and R&D-focused creators

---

## 라이선스 및 이용 안내 / License & Usage

[KR]
- 본 프로젝트는 교육 및 연구 목적의 무료 에셋입니다.
- 소스 코드 및 프로젝트 구조는 학습 및 테스트 용도로 자유롭게 활용할 수 있습니다.
- 포함된 미디어 리소스는 교육·실습 목적을 전제로 제공됩니다.
- 상업적 활용 시에는 각 리소스의 개별 라이선스를 반드시 확인해 주세요.

[EN]
- This project is provided free of charge for educational and research purposes.
- Source code and project structure may be freely used for learning and testing.
- Included media assets are intended for educational use.
- For commercial use, please verify individual asset licenses separately.

---

## 참고 사항 / Notes

본 프로젝트는
실감형 콘텐츠 제작 방식에 대한 이해를 돕기 위한
교육용 레퍼런스 프로젝트입니다.

This project is intended as an educational reference
to support immersive content creation workflows.
