using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class MyAudioSourse : MonoBehaviour
{
    [Header("Настройки моделирования")]
    //число выпускаемых лучей
    public int Количество_лучей=10;
    //максимальное число отреажения звука от препядствий
    public int Количество_отражений = 3;
    //отображение лучей в редакторе
    public bool Gizmos = true;
    //коэфициент поглащения материала препядствий
    //призводить ли графические расчеты
    public bool draw = false;
    //структра для хранения основного и мнимых источников звука
    public bool RealTimeLines = true;

    //обьект для графического представления расчетов
    public GameObject DrawObject;

    [Header("Настройки параметров звука и помещения")]
    public float Коэфициент_поглощения = 0.9f;
    //частота звука
    public int Частота = 20000;
    public float Коэфициент_поглощения_среды = 0.1f;

    //скорость звука в среде
    public float Скорость = 335;
    [Header("Временной отрезок моделирования")]
    //конец временного промежутка
    public float Конец = 5;
    //начало временного промежутка
    public float Начало = 0;
    //шаг деления временного промежутка
    public float DeltaTime = 0.1f;


    int RandAngle = 45;
    public struct AudioSourse
    {
        //кординаты источника звука
        public Vector3 point;
        //свойства препядсвия для мнимых источников звука(координаты, размер и т.д.)
        public Transform transform;
        //коэфициент поглащения звука 
        public float k;
        //расстоние от текущего отраженного источника до родительского источника
        public float distance;
        //время, необходимое звуку, чтобы добраться до точки отражения
        public float time;
        
        public AudioSourse(Vector3 a, Transform b, float c, float d, float t)
        {
            point = a;
            transform = b;
            k = c;
            distance = d;
            time = t;
        }
    };
    //список всех источников звука
    List<AudioSourse> AudioSourses=new List<AudioSourse>();
    

    int duration = 1000;

    
    //генерация лучей для трассировки
    // Vector3 point - точка из которой выпускаются лучи
    // Transform t - описание препядствия, от которого отражаютсот которого выпускаются лучи мнимых источников
    // int ReflCount - количество отражений
    // float T - время, необходимое звуку, чтобы добраться до точки из которой выпускаются лучи
    public void CreateRays(Vector3 point, Transform t, int ReflCount, float T)
    {
        if (ReflCount >= 0)
        {
            //угол отклонения лучей, генерируется случайно
            float angle = Random.Range(-45, 45);
            //расчет угла, для равномерного распредения лучей во все стороны    
            float DeltaAngle = 0;
            if (Количество_лучей > 0)
                DeltaAngle = 360 / Количество_лучей;
            for (int i = 0; i < Количество_лучей; i++)
            {
                //переменная описывающая обьект, с которым столкнулся луч
                RaycastHit hit;
                angle += DeltaAngle;
                //инициализация парметров луча - точка из которой выпускается луч и направление движения луча
                Ray ray = new Ray(point, Quaternion.AngleAxis(angle, Vector3.up) * t.rotation * (new Vector3(0, 0, 1)));
                //расчет луча и столкновений
                Physics.Raycast(ray, out hit);
                //если столкновение произошло
                if (hit.collider != null)
                {
                    //если обьект препядтсвие, взаимодействующее со звуком
                    if (hit.collider.gameObject.tag == "AudioWall")
                    {
                        //отрисовка луча в редакторе
                        if (Gizmos)
                            Debug.DrawLine(ray.origin, hit.point, new Color(1, 0, 0, 1), 1000);
                        //расчет коэфициента поглощения отражения для данной стены и дистанции от точки столкновения до точки пуска луча
                        float intenc = Mathf.Pow(Коэфициент_поглощения, Количество_отражений - ReflCount + 1);
                        float d = Distance(point.x, point.y, hit.point.x, hit.point.y);
                        if (d > 0.1f)
                        {
                            //дабавление нового мнимого источника звука с полученнымии параметрами
                            AudioSourses.Add(new AudioSourse(hit.point, hit.transform, intenc, d, d / Скорость + T));
                            //генерация лучей от точки столкновения
                            CreateRays(hit.point, hit.transform, ReflCount - 1, d / Скорость + T);
                        }
                    }

                }
            }
        }
        else
            return;
    }
    void Start()
    {
        if (RealTimeLines)
            duration = 0;

            if (AudioSourses != null)
            AudioSourses.Clear();

        GameObject[] Sourses = GameObject.FindGameObjectsWithTag("AudioSourse");
        foreach (GameObject G in Sourses)
        {
            AudioSourses.Add(new AudioSourse(G.transform.position, G.transform, 0.9f, 0, 0));
            CreateRays(G.transform.position, G.transform, Количество_отражений, 0);
        }
        
        

        //for (int i = 0; i < AudioSourses.Count; i++)
        //{
        //    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    sphere.transform.position = AudioSourses[i].point;

        //}
        if(draw)
            Draw();
       // AudioSource as = GetComponent<AudioSource>().clip.;
    }

    private void OnEnable()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
       if(RealTimeLines)
        {
            duration = 0;
            if (AudioSourses != null)
                AudioSourses.Clear();

            AudioSourses.Add(new AudioSourse(transform.position, transform, 0.9f, 0, 0));
            CreateRays(transform.position, transform, Количество_отражений, 0);
        }
       else
        duration = 1000;
    }

    //функция расчета растояния для двух точек
    float Distance(float x1, float z1, float x2, float z2)
    {
        return Vector2.Distance(new Vector2(x1, z1), new Vector2(x2, z2));
    }
    //процедура для расчета 
    void Draw()
    {
        //определение границ расчета
        Bounds b = DrawObject.GetComponent<MeshRenderer>().bounds;

        int x_start = (int)b.min.x;
        int x_end = (int)b.max.x;
        int z_start = (int)b.min.z;
        int z_end = (int)b.max.z;
        //инициализация текстуры для вывода графика
        Texture2D T = new Texture2D((int)b.size.x * 10, (int)b.size.z * 10);

        float[,] In = new float[(int)b.size.x * 10, (int)b.size.z * 10];
        //временные перменные для расчетов
        float amplitude;
        float dist;
        float intensity;
        float fPart = 0, sPart = 0;
        int X = 0, Y = 0;
        RaycastHit hit;
        Ray ray;
        int f = 0;
        bool isWall = false;

        StreamWriter myread = new StreamWriter("data.txt", false);
        string str;
        Collider c = new Collider();


        //обход временного промежутка
        for (float t = Начало; t <= Конец; t += DeltaTime)
        {
            //обход координат
            for (float x = x_start; x <= x_end; x += 0.1f)
            {
                Y = 0;
                str = "";
                for (float z = z_start; z <= z_end; z += 0.1f)
                {
                    isWall = false;
                    for (int i = 0; i < AudioSourses.Count; i++)
                    {
                        //расстояние от точки до текущего источника
                        dist = Distance(x, z, AudioSourses[i].point.x, AudioSourses[i].point.z);

                        //проверка находится ли точка в стене, если да - расчет амплитуды для нее не производится
                        
                            if(AudioSourses[i].transform.gameObject.TryGetComponent<Collider>(out c))
                                isWall = c.bounds.Contains(new Vector3(x, AudioSourses[i].point.y, z));
                        
                        //требуется ли расчет для текущего момента времени
                        if (t >= dist / Скорость + AudioSourses[i].time && !isWall)
                        {
                            //расчет луча от точки до текущего источника
                            ray = new Ray(new Vector3(x, 0, z), new Vector3(AudioSourses[i].point.x, 0, AudioSourses[i].point.z) - new Vector3(x, 0, z));
                            Physics.Raycast(ray, out hit);

                            //расстояние от точки то места столкновения с препятствием, если столкновения не было, равно нулю
                            float distT = Distance(x, z, hit.point.x, hit.point.z);
                            //если столкновения не было
                            if (hit.collider == null)
                            {
                                //расчет аплитуды для текущего источника звука
                                dist = dist  + AudioSourses[i].distance;
                                //Debug.Log(dist);
                                fPart += Mathf.Sin((dist / (Скорость / Частота)) * 2 * Mathf.PI) * AudioSourses[i].k * Mathf.Exp(-2*0.1f* dist);
                                sPart += Mathf.Cos((dist / (Скорость / Частота)) * 2 * Mathf.PI) * AudioSourses[i].k * Mathf.Exp(-2 *0.1f * dist);
                            }
                            //если препядствие находится позади источника или точка столкновения сам источник
                            else if (distT > dist || Mathf.Abs(distT - dist) < 0.1)
                            {
                                //расчет амплитуды для текущего источника звука
                                dist = dist + AudioSourses[i].distance;
                                fPart += Mathf.Sin((dist / (Скорость / Частота)) * 2 * Mathf.PI) * AudioSourses[i].k / dist;
                                sPart += Mathf.Cos((dist / (Скорость / Частота)) * 2 * Mathf.PI) * AudioSourses[i].k / dist;
                            }

                        }
                    }
                    //итоговый расчет звука
                    amplitude = Mathf.Sqrt(Mathf.Pow(fPart, 2) + Mathf.Pow(sPart, 2));
                    //если точка в стене, расчет не производится
                    if (!isWall)
                        intensity = amplitude * Mathf.Cos(2 * Mathf.PI * Частота * 1 + t);
                    else
                        intensity = 0;

                    
                    fPart = 0;
                    sPart = 0;
                    //отрисовка значения зека в пиксель текстуры
                    //In[X, Y] = intensity;

                    T.SetPixel(T.width - X, T.height - Y, new Color(intensity, intensity, intensity));
                    ++Y;

                }

                ++X;
            }
            //запись полученного изображения в файл
            T.Apply();
            SaveTextureToFile(T, "Frame_" + f + ".png");
            //++f;
        }
        DrawObject.GetComponent<MeshRenderer>().material.mainTexture = T;


        for (int x = 0; x < (int)b.size.x * 10; x++)
        {
            str = "";
            for (int z = 0; z < (int)b.size.z * 10; z++)
            {
                str += In[x, z] + ' ';
            }
            myread.WriteLine(str.TrimEnd());
        }
        myread.Close();
        Debug.Log("Done!");
    }
        //процедура для записиси тексуры в файл
        void SaveTextureToFile(Texture2D texture, string fileName)
    {
        var bytes = texture.EncodeToPNG();
        
        FileStream file = File.Open(Application.dataPath + "/Frames/" + fileName, FileMode.Create);
        var binary = new BinaryWriter(file);
        binary.Write(bytes);
        file.Close();
    }
}
