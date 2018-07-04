Shader "Custom/VertexColorUnlit" {

	Properties{

	}



	Category{

		Tags{ "Queue" = "Geometry" }

		Lighting Off

		BindChannels{

		Bind "Color", color

		Bind "Vertex", vertex

		Bind "TexCoord", texcoord

	}



		SubShader{

		Pass{
			Cull Off
	}

	}

	}

}